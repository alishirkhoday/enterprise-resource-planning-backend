using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;
using ERP.Application.UseCases.Modules.InventoryManagement.Mapping;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.WarehouseManagement;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.CreateStockCount
{
    /// <summary>
    /// Opens a count sheet - انبارگردانی - and freezes the book quantities against it.
    /// <para>
    /// The sheet is built and frozen in one step on purpose. Building it captures what the books
    /// said; freezing declares that number the one the count is measured against. Doing them apart
    /// leaves a window in which stock moves after the snapshot was taken, and every variance from
    /// that window is a lie about what the shelf held.
    /// </para>
    /// </summary>
    public class CreateStockCountCommandHandler(
        IWarehouseRepository warehouses,
        IItemInventoryRepository inventories,
        IStockCountRepository stockCounts,
        INumberingSeriesRepository numberingSeries)
        : IRequestHandler<CreateStockCountCommand, Result<StockCountDto, Error>>
    {
        private const string StockCountNumberingKey = "StockCount";

        public async Task<Result<StockCountDto, Error>> Handle(CreateStockCountCommand request, CancellationToken cancellationToken)
        {
            var warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken);
            if (warehouse is null)
                return Result.Failure<StockCountDto, Error>(Errors.General.NotFound(request.WarehouseId.ToString(), "Warehouse"));

            // Two open counts over the same shelves produce two sets of adjustments for the same
            // stock, and whichever is approved second silently undoes the first.
            var open = await stockCounts.GetOpenAsync(warehouse.Id, cancellationToken);
            if (open.Count > 0)
                return Result.Failure<StockCountDto, Error>(Errors.Warehouse.CountIsAlreadyOpen(open[0].Number));

            var lines = await ResolveInventoryAsync(request, cancellationToken);
            if (lines.Count == 0)
                return Result.Failure<StockCountDto, Error>(Errors.Warehouse.NothingToCount(warehouse.Name));

            var series = await numberingSeries.GetForUpdateAsync(StockCountNumberingKey, request.BranchId, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<StockCountDto, Error>(Errors.Order.NumberingSeriesMissing(StockCountNumberingKey));

            var count = new StockCount(series.Next(request.CountDate), warehouse.Id, request.Type, request.CountDate);

            if (request.StorageLocationId is not null)
                count.ScopeToStorageLocation(request.StorageLocationId.Value);

            if (request.CountedByHumanId is not null)
                count.AssignCounter(request.CountedByHumanId.Value);

            if (request.BranchId is not null)
                count.AssignToBranch(request.BranchId.Value);

            count.SetDescription(request.Description);

            foreach (var inventory in lines)
            {
                // Refused rather than quietly left off the sheet. An item held in a warehouse with
                // no base unit is a data error somebody has to fix, and skipping it would mean
                // nobody ever counts it.
                if (inventory.Item.BaseUnitOfMeasureId is null)
                    return Result.Failure<StockCountDto, Error>(
                        Errors.Warehouse.ItemHasNoUnitOfMeasure(inventory.Item.Name ?? inventory.ItemId.ToString()));

                var line = new StockCountItem(
                    count,
                    inventory.Item,
                    inventory.OnHandQuantity,
                    inventory.Item.BaseUnitOfMeasureId.Value,
                    inventory.AverageUnitCost);

                if (inventory.StorageLocationId is not null)
                    line.ScopeToStorageLocation(inventory.StorageLocationId.Value);

                count.AddItem(line);
            }

            try
            {
                count.Freeze();
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<StockCountDto, Error>(Errors.Warehouse.CountCannotBeChanged(exception.Message));
            }

            await stockCounts.CreateAsync(count, cancellationToken);

            return Result.Success<StockCountDto, Error>(count.ToDto());
        }

        /// <summary>
        /// What goes on the sheet. Naming no items counts the whole warehouse, which is what a
        /// full count means; naming some is a cycle or spot count of those.
        /// </summary>
        private async Task<List<ItemInventory>> ResolveInventoryAsync(CreateStockCountCommand request, CancellationToken cancellationToken)
        {
            var held = await inventories.GetByWarehouseAsync(request.WarehouseId, cancellationToken);

            var scoped = request.StorageLocationId is null
                ? held
                : [.. held.Where(i => i.StorageLocationId == request.StorageLocationId)];

            if (request.ItemIds.Count == 0)
                return [.. scoped];

            var wanted = request.ItemIds.ToHashSet();

            return [.. scoped.Where(i => wanted.Contains(i.ItemId))];
        }
    }
}
