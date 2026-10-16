using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;
using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Mapping
{
    public static class StockCountMapper
    {
        public static StockCountDto ToDto(this StockCount count)
        {
            ArgumentNullException.ThrowIfNull(count, nameof(count));

            // A sheet with no lines has no currency of its own to report, which only happens
            // between creating one and putting anything on it.
            var currencyCode = count.Items.Count > 0 ? count.Items[0].UnitCost.CurrencyCode : "IRR";

            return new StockCountDto(
                count.Id,
                count.Number,
                count.WarehouseId,
                count.Type,
                count.Status,
                count.CountDate,
                count.FrozenOn,
                count.CompletedOn,
                count.TotalSurplus,
                count.TotalShortage,
                // Surplus and shortage net off in value terms, which is the number that reaches
                // the ledger. Reporting only the two quantities hides that a large shortage of
                // something cheap can matter less than a small one of something expensive.
                count.Items.Sum(i => i.VarianceValue.Amount),
                currencyCode,
                [.. count.Items
                    .OrderBy(i => i.Item.Name)
                    .Select(i => new StockCountLineViewDto(
                        i.Id,
                        i.ItemId,
                        i.Item.Name,
                        i.SystemQuantity,
                        i.CountedQuantity,
                        i.RecountedQuantity,
                        i.Variance,
                        i.VarianceValue.Amount,
                        i.HasDiscrepancy,
                        i.DiscrepancyReason))]);
        }
    }
}
