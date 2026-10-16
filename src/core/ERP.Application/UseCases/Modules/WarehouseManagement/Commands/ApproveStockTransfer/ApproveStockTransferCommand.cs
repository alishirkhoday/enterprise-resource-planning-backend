using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;

namespace ERP.Application.UseCases.Modules.WarehouseManagement.Commands.ApproveStockTransfer
{
    public record ApproveStockTransferCommand : ApproveStockTransferDto, ICommand<Result<StockTransferDto, Error>>;
}
