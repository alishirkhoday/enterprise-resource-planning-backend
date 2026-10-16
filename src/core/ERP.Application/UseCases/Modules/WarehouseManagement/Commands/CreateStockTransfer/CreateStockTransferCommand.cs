using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;

namespace ERP.Application.UseCases.Modules.WarehouseManagement.Commands.CreateStockTransfer
{
    public record CreateStockTransferCommand : CreateStockTransferDto, ICommand<Result<StockTransferDto, Error>>;
}
