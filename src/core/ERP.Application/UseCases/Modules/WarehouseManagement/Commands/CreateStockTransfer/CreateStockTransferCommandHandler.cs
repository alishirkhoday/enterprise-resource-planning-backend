using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;
using ERP.Application.UseCases.Modules.WarehouseManagement.Mapping;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.WarehouseManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.WarehouseManagement;

namespace ERP.Application.UseCases.Modules.WarehouseManagement.Commands.CreateStockTransfer
{
    /// <summary>
    /// Raises a transfer request - حواله انتقال. Nothing moves yet.
    /// <para>
    /// The cost of each line is read from what the source warehouse is already carrying the stock
    /// at. A transfer is not a sale: moving goods between two of your own buildings must not
    /// change what they are worth, and taking the cost from anywhere else would.
    /// </para>
    /// </summary>
    public class CreateStockTransferCommandHandler(
        IWarehouseRepository warehouses,
        IItemRepository items,
        IItemInventoryRepository inventories,
        IStockTransferOrderRepository transfers,
        INumberingSeriesRepository numberingSeries)
        : IRequestHandler<CreateStockTransferCommand, Result<StockTransferDto, Error>>
    {
        private const string TransferNumberingKey = "StockTransfer";

        public async Task<Result<StockTransferDto, Error>> Handle(CreateStockTransferCommand request, CancellationToken cancellationToken)
        {
            if (request.SourceWarehouseId == request.DestinationWarehouseId)
                return Result.Failure<StockTransferDto, Error>(Errors.Warehouse.SameSourceAndDestination(request.SourceWarehouseId.ToString()));

            var source = await warehouses.GetByIdAsync(request.SourceWarehouseId, cancellationToken);
            if (source is null)
                return Result.Failure<StockTransferDto, Error>(Errors.General.NotFound(request.SourceWarehouseId.ToString(), nameof(Warehouse)));

            var destination = await warehouses.GetByIdAsync(request.DestinationWarehouseId, cancellationToken);
            if (destination is null)
                return Result.Failure<StockTransferDto, Error>(Errors.General.NotFound(request.DestinationWarehouseId.ToString(), nameof(Warehouse)));

            var currencyCode = request.CurrencyCode.Trim().ToUpperInvariant();

            var series = await numberingSeries.GetForUpdateAsync(TransferNumberingKey, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<StockTransferDto, Error>(Errors.Order.NumberingSeriesMissing(TransferNumberingKey));

            var transfer = new StockTransferOrder(series.Next(request.RequestDate), source, destination, request.RequestDate, currencyCode);

            if (request.ExpectedArrivalDate is not null)
                transfer.SetExpectedArrivalDate(request.ExpectedArrivalDate.Value);

            if (request.RequestedByHumanId is not null)
                transfer.SetRequester(request.RequestedByHumanId.Value);

            transfer.SetVehicleNumber(request.VehicleNumber);
            transfer.SetDescription(request.Description);

            var lines = await BuildLinesAsync(transfer, source, request, currencyCode, cancellationToken);
            if (lines.IsFailure)
                return Result.Failure<StockTransferDto, Error>(lines.Error);

            foreach (var line in lines.Value)
            {
                transfer.AddItem(line);
            }

            await transfers.CreateAsync(transfer, cancellationToken);

            return Result.Success<StockTransferDto, Error>(transfer.ToDto());
        }

        private async Task<Result<List<StockTransferOrderItem>, Error>> BuildLinesAsync(
            StockTransferOrder transfer,
            Warehouse source,
            CreateStockTransferCommand request,
            string currencyCode,
            CancellationToken cancellationToken)
        {
            var built = new List<StockTransferOrderItem>(request.Lines.Count);
            var found = await items.GetByIdsAsync([.. request.Lines.Select(l => l.ItemId)], cancellationToken);
            var byId = found.ToDictionary(i => i.Id);

            short lineNumber = 1;

            foreach (var line in request.Lines)
            {
                if (!byId.TryGetValue(line.ItemId, out var item))
                    return Result.Failure<List<StockTransferOrderItem>, Error>(Errors.General.NotFound(line.ItemId.ToString(), nameof(Item)));

                var inventory = await inventories.GetAsync(item.Id, source.Id, line.SourceStorageLocationId, cancellationToken);
                if (inventory is null)
                    return Result.Failure<List<StockTransferOrderItem>, Error>(
                        Errors.Warehouse.NoInventoryRecord(item.Name ?? item.Id.ToString(), source.Name));

                // Checked now as well as at dispatch. Raising a request for stock that is not
                // there wastes somebody's afternoon before anybody walks to the shelf.
                if (line.Quantity > inventory.AvailableQuantity)
                    return Result.Failure<List<StockTransferOrderItem>, Error>(
                        Errors.Warehouse.InsufficientStock(item.Name ?? item.Id.ToString(), line.Quantity, inventory.AvailableQuantity));

                var unitCost = line.UnitCost is not null
                    ? Money.Create(line.UnitCost.Value, currencyCode)
                    : inventory.AverageUnitCost;

                var transferLine = new StockTransferOrderItem(transfer, item, lineNumber++, line.Quantity, line.UnitOfMeasureId, unitCost);

                if (line.ItemBatchId is not null)
                    transferLine.SetBatch(line.ItemBatchId.Value);

                transferLine.SetStorageLocations(line.SourceStorageLocationId, line.DestinationStorageLocationId);

                built.Add(transferLine);
            }

            return Result.Success<List<StockTransferOrderItem>, Error>(built);
        }
    }
}
