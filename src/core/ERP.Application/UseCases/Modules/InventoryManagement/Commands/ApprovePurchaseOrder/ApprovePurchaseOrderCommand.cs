using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;

namespace ERP.Application.UseCases.Modules.InventoryManagement.Commands.ApprovePurchaseOrder
{
    /// <summary>
    /// Approves or rejects a submitted order. The two live in one command because they are the
    /// same decision with two outcomes, and a rejection needs the reason an approval does not.
    /// </summary>
    public sealed record ApprovePurchaseOrderCommand : ICommand<Result<PurchaseOrderDto, Error>>
    {
        public Guid PurchaseOrderId { get; init; }

        public bool IsApproved { get; init; }

        public string? Reason { get; init; }

        public Guid? ApprovalRequestId { get; init; }
    }
}
