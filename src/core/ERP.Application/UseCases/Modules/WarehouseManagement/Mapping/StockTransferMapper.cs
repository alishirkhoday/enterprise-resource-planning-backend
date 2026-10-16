using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;
using ERP.Domain.Entities.Modules.WarehouseManagement;

namespace ERP.Application.UseCases.Modules.WarehouseManagement.Mapping
{
    public static class StockTransferMapper
    {
        public static StockTransferDto ToDto(this StockTransferOrder transfer)
        {
            ArgumentNullException.ThrowIfNull(transfer, nameof(transfer));

            return new StockTransferDto(
                transfer.Id,
                transfer.Number,
                transfer.SourceWarehouseId,
                transfer.SourceWarehouse.Name,
                transfer.DestinationWarehouseId,
                transfer.DestinationWarehouse.Name,
                transfer.Status,
                transfer.RequestDate,
                transfer.ExpectedArrivalDate,
                transfer.DispatchedOn,
                transfer.ReceivedOn,
                transfer.CurrencyCode,
                transfer.HasDiscrepancy,
                [.. transfer.Items
                    .OrderBy(i => i.LineNumber)
                    .Select(i => new StockTransferLineViewDto(
                        i.Id,
                        i.LineNumber,
                        i.ItemId,
                        i.Item.Name,
                        i.RequestedQuantity,
                        i.DispatchedQuantity,
                        i.ReceivedQuantity,
                        i.ShortageQuantity,
                        i.UnitCost.Amount,
                        i.DiscrepancyReason))]);
        }
    }
}
