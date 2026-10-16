using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;
using ERP.Application.UseCases.Modules.InventoryManagement.Mapping;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.ApprovePurchaseOrder
{
    /// <summary>
    /// Records the decision on a submitted order. Approving it raises the fact, which is what a
    /// supplier notification and a commitment against the budget both hang from.
    /// </summary>
    public class ApprovePurchaseOrderCommandHandler(IPurchaseOrderRepository purchaseOrders)
        : IRequestHandler<ApprovePurchaseOrderCommand, Result<PurchaseOrderDto, Error>>
    {
        public async Task<Result<PurchaseOrderDto, Error>> Handle(ApprovePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await purchaseOrders.GetWithItemsAsync(request.PurchaseOrderId, cancellationToken);
            if (order is null)
                return Result.Failure<PurchaseOrderDto, Error>(Errors.General.NotFound(request.PurchaseOrderId.ToString(), nameof(PurchaseOrder)));

            try
            {
                if (request.IsApproved)
                    order.Approve(request.ApprovalRequestId);
                else
                    order.Reject(request.Reason!);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<PurchaseOrderDto, Error>(Errors.Purchasing.OrderCannotBeDecided(exception.Message));
            }

            await purchaseOrders.UpdateAsync(order, cancellationToken);

            return Result.Success<PurchaseOrderDto, Error>(order.ToDto());
        }
    }
}
