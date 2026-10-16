using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.RecordStockCount
{
    public record RecordStockCountCommand : RecordStockCountDto, ICommand<Result<StockCountDto, Error>>;
}
