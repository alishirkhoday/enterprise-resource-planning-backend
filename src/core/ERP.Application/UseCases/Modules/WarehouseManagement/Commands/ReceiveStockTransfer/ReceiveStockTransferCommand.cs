using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;

namespace ERP.Application.UseCases.Modules.WarehouseManagement.Commands.ReceiveStockTransfer
{
    public record ReceiveStockTransferCommand : ReceiveStockTransferDto, ICommand<Result<StockTransferDto, Error>>;
}
