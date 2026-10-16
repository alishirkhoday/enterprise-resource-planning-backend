namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// The record of goods physically arriving against a purchase order. Confirming it is what moves
/// stock and writes the inventory ledger.
/// </summary>
public sealed class GoodsReceipt : BaseEntity, IBranchScoped
{
    public string Number { get; private set; } = default!;

    public PurchaseOrder PurchaseOrder { get; private set; } = default!;
    public Guid PurchaseOrderId { get; private set; }

    public Guid WarehouseId { get; private set; }
    public Guid? StorageLocationId { get; private set; }

    public DateOnly ReceiptDate { get; private set; }
    public GoodsReceiptStatus Status { get; private set; }

    /// <summary>The delivery note number the supplier handed over with the goods.</summary>
    public string? DeliveryNoteNumber { get; private set; }
    public string? VehicleNumber { get; private set; }
    public Guid? ReceivedByHumanId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string? Description { get; private set; }

    public DateTimeOffset? ConfirmedOn { get; private set; }

    private readonly List<GoodsReceiptItem> _items = [];
    public IReadOnlyList<GoodsReceiptItem> Items => _items;

    private GoodsReceipt() { }

    public GoodsReceipt(string number, PurchaseOrder purchaseOrder, Guid warehouseId, DateOnly receiptDate)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrder, nameof(purchaseOrder));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId is empty.", nameof(warehouseId));
        if (!purchaseOrder.CanReceive)
            throw new InvalidOperationException($"Purchase order {purchaseOrder.Number} is {purchaseOrder.Status} and cannot be received against.");

        Number = number.Trim();
        PurchaseOrder = purchaseOrder;
        PurchaseOrderId = purchaseOrder.Id;
        WarehouseId = warehouseId;
        ReceiptDate = receiptDate;
        Status = GoodsReceiptStatus.Draft;
    }

    public void SetDeliveryDetails(string? deliveryNoteNumber, string? vehicleNumber)
    {
        DeliveryNoteNumber = string.IsNullOrWhiteSpace(deliveryNoteNumber) ? null : deliveryNoteNumber.Trim();
        VehicleNumber = string.IsNullOrWhiteSpace(vehicleNumber) ? null : vehicleNumber.Trim();
    }

    public void ScopeToStorageLocation(Guid storageLocationId) =>
        StorageLocationId = storageLocationId == Guid.Empty ? null : storageLocationId;

    public void SetReceiver(Guid humanId) => ReceivedByHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddItem(GoodsReceiptItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureDraft();
        if (item.OrderItem.PurchaseOrderId != PurchaseOrderId)
            throw new InvalidOperationException("The line belongs to a different purchase order.");
        _items.Add(item);
    }

    public void RemoveItem(GoodsReceiptItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureDraft();
        _items.Remove(item);
    }

    /// <summary>
    /// Confirms the receipt, posting the accepted quantities back onto the order lines. The
    /// caller is expected to write the matching stock ledger entries in the same transaction.
    /// </summary>
    public void Confirm()
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("A goods receipt cannot be confirmed without lines.");

        foreach (var item in _items)
        {
            if (item.AcceptedQuantity > 0m)
                item.OrderItem.Receive(item.AcceptedQuantity);
        }

        PurchaseOrder.RefreshReceiptStatus();
        Status = GoodsReceiptStatus.Confirmed;
        ConfirmedOn = DateTimeOffset.UtcNow;
        Raise(new GoodsReceived(Id, PurchaseOrderId, WarehouseId));
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == GoodsReceiptStatus.Confirmed)
        {
            foreach (var item in _items.Where(i => i.AcceptedQuantity > 0m))
            {
                item.OrderItem.ReverseReceipt(item.AcceptedQuantity);
            }
            PurchaseOrder.RefreshReceiptStatus();
        }
        Status = GoodsReceiptStatus.Cancelled;
        Description = reason.Trim();
    }

    public decimal TotalAccepted => _items.Sum(i => i.AcceptedQuantity);
    public decimal TotalRejected => _items.Sum(i => i.RejectedQuantity);

    private void EnsureDraft()
    {
        if (Status != GoodsReceiptStatus.Draft)
            throw new InvalidOperationException($"The receipt is {Status} and can no longer be edited.");
    }
}
