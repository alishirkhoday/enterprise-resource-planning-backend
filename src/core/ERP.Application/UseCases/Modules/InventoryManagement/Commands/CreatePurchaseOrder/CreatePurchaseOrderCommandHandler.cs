using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;
using ERP.Application.UseCases.Modules.InventoryManagement.Mapping;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.CreatePurchaseOrder
{
    /// <summary>
    /// Raises a purchase order as a draft.
    /// <para>
    /// A line without a price falls back to the framework agreement with this supplier, which is
    /// the reason for having one: the price was negotiated once and should not be retyped, and
    /// mistyped, on every order.
    /// </para>
    /// </summary>
    public class CreatePurchaseOrderCommandHandler(
        ISupplierRepository suppliers,
        IItemRepository items,
        IPurchaseContractRepository contracts,
        IPurchaseOrderRepository purchaseOrders,
        INumberingSeriesRepository numberingSeries,
        TimeProvider timeProvider)
        : IRequestHandler<CreatePurchaseOrderCommand, Result<PurchaseOrderDto, Error>>
    {
        private const string PurchaseOrderNumberingKey = "PurchaseOrder";

        public async Task<Result<PurchaseOrderDto, Error>> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            var currencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

            var supplier = await suppliers.GetByIdAsync(request.SupplierId, cancellationToken);
            if (supplier is null)
                return Result.Failure<PurchaseOrderDto, Error>(Errors.General.NotFound(request.SupplierId.ToString(), nameof(Supplier)));

            if (!supplier.CanBeOrderedFrom)
                return Result.Failure<PurchaseOrderDto, Error>(Errors.Purchasing.SupplierCannotBeOrderedFrom(supplier.Name));

            var contract = await contracts.GetEffectiveForItemAsync(supplier.Id, Guid.Empty, today, cancellationToken);

            var series = await numberingSeries.GetForUpdateAsync(PurchaseOrderNumberingKey, request.BranchId, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<PurchaseOrderDto, Error>(Errors.Order.NumberingSeriesMissing(PurchaseOrderNumberingKey));

            var order = new PurchaseOrder(series.Next(today), supplier, currencyCode);

            var lines = await BuildLinesAsync(order, request, supplier.Id, today, currencyCode, cancellationToken);
            if (lines.IsFailure)
                return Result.Failure<PurchaseOrderDto, Error>(lines.Error);

            if (request.ExpectedDeliveryDate is not null)
                order.SetExpectedDeliveryDate(request.ExpectedDeliveryDate.Value);

            if (request.WarehouseId is not null)
                order.SetDeliveryTarget(request.WarehouseId.Value);

            if (request.PaymentTermId is not null)
                order.SetPaymentTerm(request.PaymentTermId.Value);

            if (request.CostCenterId is not null)
                order.AssignCostCenter(request.CostCenterId.Value);

            if (request.BranchId is not null)
                order.AssignToBranch(request.BranchId.Value);

            if (request.AdditionalCosts > 0m)
                order.SetAdditionalCosts(Money.Create(request.AdditionalCosts, currencyCode));

            order.SetDescription(request.Description);

            await purchaseOrders.CreateAsync(order, cancellationToken);

            return Result.Success<PurchaseOrderDto, Error>(order.ToDto());
        }

        private async Task<Result<List<PurchaseOrderItem>, Error>> BuildLinesAsync(
            PurchaseOrder order,
            CreatePurchaseOrderCommand request,
            Guid supplierId,
            DateOnly today,
            string currencyCode,
            CancellationToken cancellationToken)
        {
            var built = new List<PurchaseOrderItem>(request.Lines.Count);
            var found = await items.GetByIdsAsync([.. request.Lines.Select(l => l.ItemId)], cancellationToken);
            var byId = found.ToDictionary(i => i.Id);

            short lineNumber = 1;

            foreach (var line in request.Lines)
            {
                if (!byId.TryGetValue(line.ItemId, out var item))
                    return Result.Failure<List<PurchaseOrderItem>, Error>(Errors.General.NotFound(line.ItemId.ToString(), nameof(Item)));

                if (!item.IsPurchasable)
                    return Result.Failure<List<PurchaseOrderItem>, Error>(Errors.Purchasing.ItemIsNotPurchasable(item.Name ?? line.ItemId.ToString()));

                var price = await ResolvePriceAsync(line, item, supplierId, today, currencyCode, cancellationToken);
                if (price.IsFailure)
                    return Result.Failure<List<PurchaseOrderItem>, Error>(price.Error);

                var orderItem = new PurchaseOrderItem(order, item, lineNumber++, line.Quantity, line.UnitOfMeasureId, price.Value);
                order.AddItem(orderItem);
                built.Add(orderItem);

                if (line.DiscountPercentage is not null)
                    order.SetPurchaseOrderItemDiscount(orderItem.Id, Percentage.Create(line.DiscountPercentage.Value));

                if (line.TaxId is not null && line.TaxPercentage is not null)
                    order.SetPurchaseOrderItemTax(orderItem.Id, line.TaxId.Value, Percentage.Create(line.TaxPercentage.Value));

                if (line.ExpectedDeliveryDate is not null)
                    order.SetPurchaseOrderItemExpectedDeliveryDate(orderItem.Id, line.ExpectedDeliveryDate.Value);

                order.SetPurchaseOrderItemDescription(orderItem.Id, line.Description);
            }

            return Result.Success<List<PurchaseOrderItem>, Error>(built);
        }

        /// <summary>
        /// The price the caller typed, or failing that the one the contract fixed. A line with
        /// neither is refused rather than ordered at nothing.
        /// </summary>
        private async Task<Result<Money, Error>> ResolvePriceAsync(
            PurchaseOrderLineDto line,
            Item item,
            Guid supplierId,
            DateOnly today,
            string currencyCode,
            CancellationToken cancellationToken)
        {
            if (line.UnitPrice is not null)
                return Result.Success<Money, Error>(Money.Create(line.UnitPrice.Value, currencyCode));

            var contract = await contracts.GetEffectiveForItemAsync(supplierId, item.Id, today, cancellationToken);
            var agreed = contract?.PriceFor(item.Id, today);

            return agreed is null
                ? Result.Failure<Money, Error>(Errors.Purchasing.NoPriceForItem(item.Name ?? item.Id.ToString()))
                : Result.Success<Money, Error>(agreed);
        }
    }
}
