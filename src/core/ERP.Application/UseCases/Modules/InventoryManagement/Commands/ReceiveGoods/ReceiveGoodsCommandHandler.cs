using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.ReceiveGoods
{
    /// <summary>
    /// Books goods in against a purchase order: the receipt, the order lines it satisfies, and the
    /// stock it puts on the shelf.
    /// <para>
    /// All three move together or none do. Confirming a receipt without raising the inventory
    /// leaves goods that exist on paper and not in the system; raising inventory without the
    /// receipt leaves stock nobody can trace to a delivery. The unit of work is what holds them
    /// together, so this does the work and lets the pipeline commit it once.
    /// </para>
    /// </summary>
    public class ReceiveGoodsCommandHandler(
        IPurchaseOrderRepository purchaseOrders,
        IItemInventoryRepository inventories,
        IStockLedgerEntryRepository stockLedger,
        INumberingSeriesRepository numberingSeries,
        TimeProvider timeProvider)
        : IRequestHandler<ReceiveGoodsCommand, Result<GoodsReceiptDto, Error>>
    {
        private const string GoodsReceiptNumberingKey = "GoodsReceipt";

        public async Task<Result<GoodsReceiptDto, Error>> Handle(ReceiveGoodsCommand request, CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

            var order = await purchaseOrders.GetWithItemsAsync(request.PurchaseOrderId, cancellationToken);
            if (order is null)
                return Result.Failure<GoodsReceiptDto, Error>(Errors.General.NotFound(request.PurchaseOrderId.ToString(), nameof(PurchaseOrder)));

            if (order.Status is not (PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived))
                return Result.Failure<GoodsReceiptDto, Error>(Errors.Purchasing.OrderIsNotReceivable(order.Status.ToString()));

            var series = await numberingSeries.GetForUpdateAsync(GoodsReceiptNumberingKey, order.BranchId, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<GoodsReceiptDto, Error>(Errors.Order.NumberingSeriesMissing(GoodsReceiptNumberingKey));

            var receipt = new GoodsReceipt(series.Next(today), order, request.WarehouseId, request.ReceiptDate);
            receipt.SetDeliveryDetails(request.DeliveryNoteNumber, request.VehicleNumber);

            if (request.StorageLocationId is not null)
                receipt.ScopeToStorageLocation(request.StorageLocationId.Value);

            if (request.ReceivedByHumanId is not null)
                receipt.SetReceiver(request.ReceivedByHumanId.Value);

            if (order.BranchId is not null)
                receipt.AssignToBranch(order.BranchId.Value);

            receipt.SetDescription(request.Description);

            var built = BuildLines(receipt, order, request);
            if (built.IsFailure)
                return Result.Failure<GoodsReceiptDto, Error>(built.Error);

            foreach (var line in built.Value)
            {
                receipt.AddItem(line);
            }

            try
            {
                // Confirming is what tells the order lines they have been satisfied and refreshes
                // its receipt status, so the stock movements below are the only part left.
                receipt.Confirm();
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<GoodsReceiptDto, Error>(Errors.Purchasing.ReceiptCannotBeConfirmed(exception.Message));
            }

            var posted = await PostToStockAsync(receipt, request.WarehouseId, request.StorageLocationId, cancellationToken);
            if (posted.IsFailure)
                return Result.Failure<GoodsReceiptDto, Error>(posted.Error);

            await purchaseOrders.UpdateAsync(order, cancellationToken);

            return Result.Success<GoodsReceiptDto, Error>(new GoodsReceiptDto(
                receipt.Id,
                receipt.Number,
                order.Id,
                order.Number,
                receipt.WarehouseId,
                receipt.ReceiptDate,
                receipt.Status,
                [.. receipt.Items.Select(i => new GoodsReceiptLineViewDto(
                    i.Id,
                    i.OrderItem.ItemId,
                    i.OrderItem.Item.Name,
                    i.ReceivedQuantity,
                    i.AcceptedQuantity,
                    i.RejectedQuantity,
                    i.UnitCost.Amount))]));
        }

        private static Result<List<GoodsReceiptItem>, Error> BuildLines(
            GoodsReceipt receipt,
            PurchaseOrder order,
            ReceiveGoodsCommand request)
        {
            var built = new List<GoodsReceiptItem>(request.Lines.Count);
            var orderLines = order.Items.ToDictionary(i => i.Id);

            foreach (var line in request.Lines)
            {
                if (!orderLines.TryGetValue(line.PurchaseOrderItemId, out var orderLine))
                    return Result.Failure<List<GoodsReceiptItem>, Error>(Errors.Purchasing.LineIsNotOnTheOrder(line.PurchaseOrderItemId));

                // A delivery bigger than what is still outstanding is either a duplicate entry or
                // a supplier shipping more than was ordered. Both are decisions for a person.
                var outstanding = orderLine.Quantity - orderLine.ReceivedQuantity;
                if (line.ReceivedQuantity > outstanding)
                    return Result.Failure<List<GoodsReceiptItem>, Error>(
                        Errors.Purchasing.MoreReceivedThanOrdered(orderLine.Item.Name ?? orderLine.ItemId.ToString(), line.ReceivedQuantity, outstanding));

                var unitCost = line.UnitCost is not null
                    ? Money.Create(line.UnitCost.Value, order.CurrencyCode)
                    : orderLine.UnitPrice;

                var receiptLine = new GoodsReceiptItem(receipt, orderLine, line.ReceivedQuantity, unitCost);

                if (line.RejectedQuantity > 0m)
                    receiptLine.Reject(line.RejectedQuantity, line.RejectionReason ?? "Rejected on inspection.");

                if (line.ItemBatchId is not null)
                    receiptLine.SetBatch(line.ItemBatchId.Value);

                if (request.StorageLocationId is not null)
                    receiptLine.ScopeToStorageLocation(request.StorageLocationId.Value);

                built.Add(receiptLine);
            }

            return Result.Success<List<GoodsReceiptItem>, Error>(built);
        }

        /// <summary>
        /// Raises the inventory and writes the ledger entry that explains it. Only what was
        /// accepted goes on the shelf; a rejected quantity was delivered but never became stock.
        /// </summary>
        private async Task<Result<bool, Error>> PostToStockAsync(
            GoodsReceipt receipt,
            Guid warehouseId,
            Guid? storageLocationId,
            CancellationToken cancellationToken)
        {
            foreach (var line in receipt.Items.Where(i => i.AcceptedQuantity > 0m))
            {
                var item = line.OrderItem.Item;

                var inventory = await inventories.GetForUpdateAsync(item.Id, warehouseId, storageLocationId, cancellationToken);
                if (inventory is null)
                    return Result.Failure<bool, Error>(Errors.Purchasing.NoInventoryRecord(item.Name ?? item.Id.ToString()));

                var value = line.UnitCost.Multiply(line.AcceptedQuantity);
                inventory.Increase(line.AcceptedQuantity, value);

                var entry = new StockLedgerEntry(
                    item,
                    warehouseId,
                    StockMovementType.PurchaseReceipt,
                    line.AcceptedQuantity,
                    value,
                    line.OrderItem.UnitOfMeasureId,
                    inventory.OnHandQuantity,
                    inventory.TotalValue,
                    DateTimeOffset.UtcNow);

                entry.SetTracking(line.ItemBatchId, null);

                if (storageLocationId is not null)
                    entry.ScopeToStorageLocation(storageLocationId.Value);

                await stockLedger.CreateAsync(entry, cancellationToken);
            }

            return Result.Success<bool, Error>(true);
        }
    }
}
