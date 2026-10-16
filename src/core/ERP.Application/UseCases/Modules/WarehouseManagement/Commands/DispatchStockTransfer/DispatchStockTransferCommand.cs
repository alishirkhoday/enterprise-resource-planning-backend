using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;

namespace ERP.Application.UseCases.Modules.WarehouseManagement.Commands.DispatchStockTransfer
{
    public record DispatchStockTransferCommand : DispatchStockTransferDto, ICommand<Result<StockTransferDto, Error>>;
}
