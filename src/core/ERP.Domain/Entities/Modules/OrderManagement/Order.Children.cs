using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.OrderManagement.ValueObjects;

namespace ERP.Domain.Entities.Modules.OrderManagement;

public sealed partial class Order
{
    public void LinkRefundToReturnRequest(Guid childId, Guid returnRequestId)
    {
        var child = GetChild(childId, _payments.SelectMany(parent => parent.Refunds));
        child.LinkToReturnRequest(returnRequestId);
    }

    public void SetRefundRequester(Guid childId, Guid userId)
    {
        var child = GetChild(childId, _payments.SelectMany(parent => parent.Refunds));
        child.SetRequester(userId);
    }

    public void SetRefundDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _payments.SelectMany(parent => parent.Refunds));
        child.SetDescription(description);
    }

    public void ApproveRefund(Guid childId)
    {
        var child = GetChild(childId, _payments.SelectMany(parent => parent.Refunds));
        child.Approve();
    }

    public void MarkRefundAsProcessed(Guid childId, string transactionId, Guid? settlementId = null)
    {
        var child = GetChild(childId, _payments.SelectMany(parent => parent.Refunds));
        child.MarkAsProcessed(transactionId, settlementId);
    }

    public void RejectRefund(Guid childId, string reason)
    {
        var child = GetChild(childId, _payments.SelectMany(parent => parent.Refunds));
        child.Reject(reason);
    }

    public void SetOrderItemDiscount(Guid childId, Money discount)
    {
        EnsurePending();
        var child = GetChild(childId, _items);
        child.SetDiscount(discount);
    }

    public void SetOrderItemTax(Guid childId, Guid taxId, Percentage taxPercentage)
    {
        EnsurePending();
        var child = GetChild(childId, _items);
        child.SetTax(taxId, taxPercentage);
    }

    public void SetOrderItemBatch(Guid childId, Guid itemBatchId)
    {
        EnsurePending();
        var child = GetChild(childId, _items);
        child.SetBatch(itemBatchId);
    }

    public void SetOrderItemDescription(Guid childId, string? description)
    {
        EnsurePending();
        var child = GetChild(childId, _items);
        child.SetDescription(description);
    }

    public void SetPaymentGatewayInformation(Guid childId, PaymentGatewayInformation information)
    {
        var child = GetChild(childId, _payments);
        child.SetGatewayInformation(information);
    }

    public void SetPaymentAuthority(Guid childId, string authorityCode)
    {
        var child = GetChild(childId, _payments);
        child.SetAuthority(authorityCode);
    }

    public void LinkPaymentToInstallmentPlan(Guid childId, Guid installmentPlanId)
    {
        var child = GetChild(childId, _payments);
        child.LinkToInstallmentPlan(installmentPlanId);
    }

    public void LinkPaymentToWallet(Guid childId, Guid walletId)
    {
        var child = GetChild(childId, _payments);
        child.LinkToWallet(walletId);
    }

    public void MarkPaymentAsSucceeded(Guid childId, Money amountPaid, string transactionId)
    {
        if (Status is OrderStatus.Cancelled or OrderStatus.Completed)
            throw new InvalidOperationException("The aggregate state does not allow this child operation.");
        var child = GetChild(childId, _payments);
        child.MarkAsSucceeded(amountPaid, transactionId);
    }

    public void MarkPaymentAsFailed(Guid childId, string reason)
    {
        if (Status is OrderStatus.Cancelled or OrderStatus.Completed)
            throw new InvalidOperationException("The aggregate state does not allow this child operation.");
        var child = GetChild(childId, _payments);
        child.MarkAsFailed(reason);
    }

    public void LinkPaymentToSettlement(Guid childId, Guid settlementId)
    {
        var child = GetChild(childId, _payments);
        child.LinkToSettlement(settlementId);
    }

    public void AddPaymentRefund(Guid childId, Refund refund)
    {
        var child = GetChild(childId, _payments);
        child.AddRefund(refund);
    }

    public void SetOrderShipmentStatusHistoryDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _shipmentStatusHistories);
        child.SetDescription(description);
    }

    public void SetOrderShipmentStatusHistoryChangedBy(Guid childId, Guid userId)
    {
        var child = GetChild(childId, _shipmentStatusHistories);
        child.SetChangedBy(userId);
    }
}
