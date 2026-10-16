using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.CreateStockCount
{
    public record CreateStockCountCommand : CreateStockCountDto, ICommand<Result<StockCountDto, Error>>;
}
