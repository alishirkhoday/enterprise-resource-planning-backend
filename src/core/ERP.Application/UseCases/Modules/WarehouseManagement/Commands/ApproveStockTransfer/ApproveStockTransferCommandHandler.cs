using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;
using ERP.Application.UseCases.Modules.WarehouseManagement.Mapping;
using ERP.Domain.Entities.Modules.WarehouseManagement;
using ERP.Domain.Repositories.Modules.WarehouseManagement;

namespace ERP.Application.UseCases.Modules.WarehouseManagement.Commands.ApproveStockTransfer
{
    /// <summary>
    /// Approves a transfer request. Still nothing moves: approval is permission to dispatch, and
    /// the stock only leaves when somebody says it did.
    /// </summary>
    public class ApproveStockTransferCommandHandler(IStockTransferOrderRepository transfers)
        : IRequestHandler<ApproveStockTransferCommand, Result<StockTransferDto, Error>>
    {
        public async Task<Result<StockTransferDto, Error>> Handle(ApproveStockTransferCommand request, CancellationToken cancellationToken)
        {
            var transfer = await transfers.GetWithItemsAsync(request.TransferId, cancellationToken);
            if (transfer is null)
                return Result.Failure<StockTransferDto, Error>(Errors.General.NotFound(request.TransferId.ToString(), nameof(StockTransferOrder)));

            try
            {
                transfer.Approve();
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<StockTransferDto, Error>(Errors.Warehouse.TransferCannotBeChanged(exception.Message));
            }

            await transfers.UpdateAsync(transfer, cancellationToken);

            return Result.Success<StockTransferDto, Error>(transfer.ToDto());
        }
    }
}
