using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.WarehouseManagement;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Queries.GetItemStockCard
{
    /// <summary>
    /// The stock card - کاردکس کالا.
    /// <para>
    /// The opening balance is read from the ledger as it stood the instant before the window,
    /// not derived by subtracting the period's movements from today. The two agree when nothing
    /// is wrong, and when something is wrong the derived one hides it: a card that always closes
    /// is a card that cannot tell you the ledger has drifted.
    /// </para>
    /// </summary>
    public class GetItemStockCardQueryHandler(
        IItemRepository items,
        IWarehouseRepository warehouses,
        IStockLedgerEntryRepository stockLedger,
        TimeProvider timeProvider)
        : IRequestHandler<GetItemStockCardQuery, Result<ItemStockCardDto, Error>>
    {
        public async Task<Result<ItemStockCardDto, Error>> Handle(GetItemStockCardQuery request, CancellationToken cancellationToken)
        {
            var item = await items.GetByIdAsync(request.ItemId, cancellationToken);
            if (item is null)
                return Result.Failure<ItemStockCardDto, Error>(Errors.General.NotFound(request.ItemId.ToString(), nameof(Item)));

            var warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken);
            if (warehouse is null)
                return Result.Failure<ItemStockCardDto, Error>(Errors.General.NotFound(request.WarehouseId.ToString(), "Warehouse"));

            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            var to = request.To ?? today;
            var from = request.From ?? to.AddMonths(-1);

            if (from > to)
                return Result.Failure<ItemStockCardDto, Error>(Errors.General.AnErrorHasOccurred());

            var windowStart = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

            // The window ends at the last instant of its closing day, so a movement made this
            // afternoon appears on a card asked for up to today.
            var windowEnd = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

            var opening = await stockLedger.GetBalanceAtAsync(item.Id, warehouse.Id, windowStart, cancellationToken);
            var movements = await stockLedger.GetCardAsync(item.Id, warehouse.Id, windowStart, windowEnd, cancellationToken);

            var currencyCode = movements.Count > 0
                ? movements[0].ValueChange.CurrencyCode
                : "IRR";

            var closing = movements.Count > 0
                ? (movements[^1].BalanceQuantity, movements[^1].BalanceValue.Amount)
                : (opening.Quantity, opening.Value);

            return Result.Success<ItemStockCardDto, Error>(new ItemStockCardDto(
                item.Id,
                item.Name,
                item.StockKeepingUnit,
                warehouse.Id,
                warehouse.Name,
                from,
                to,
                currencyCode,
                opening.Quantity,
                opening.Value,
                movements.Where(m => m.QuantityChange > 0m).Sum(m => m.QuantityChange),
                movements.Where(m => m.QuantityChange < 0m).Sum(m => Math.Abs(m.QuantityChange)),
                closing.Item1,
                closing.Item2,
                [.. movements.Select(m => new ItemStockCardLineDto(
                    m.Id,
                    m.MovementDateTime,
                    m.MovementType,
                    m.QuantityChange > 0m ? m.QuantityChange : 0m,
                    m.QuantityChange < 0m ? Math.Abs(m.QuantityChange) : 0m,
                    m.UnitCost.Amount,
                    m.ValueChange.Amount,
                    m.BalanceQuantity,
                    m.BalanceValue.Amount,
                    m.ReferenceType,
                    m.ReferenceNumber,
                    m.ItemBatchId,
                    m.Description))]));
        }
    }
}
