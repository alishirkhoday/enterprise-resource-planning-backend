using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.ApproveStockCount
{
    public record ApproveStockCountCommand : ApproveStockCountDto, ICommand<Result<StockCountDto, Error>>;
}
