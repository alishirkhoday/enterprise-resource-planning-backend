namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// An internal request to buy something. It states the need; purchasing decides from whom and
/// at what price.
/// </summary>
public sealed class PurchaseRequisition : BaseEntity, IBranchScoped
{
    public string Number { get; private set; } = default!;
    public DateOnly RequestDate { get; private set; }
    public DateOnly? RequiredByDate { get; private set; }
    public PurchaseRequisitionStatus Status { get; private set; }
    public RequisitionPriority Priority { get; private set; }

    public Guid RequestedByHumanId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? ApprovalRequestId { get; private set; }

    public string? Justification { get; private set; }
    public string? RejectionReason { get; private set; }

    private readonly List<PurchaseRequisitionItem> _items = [];
    public IReadOnlyList<PurchaseRequisitionItem> Items => _items;

    private PurchaseRequisition() { }

    public PurchaseRequisition(string number, Guid requestedByHumanId, DateOnly requestDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        if (requestedByHumanId == Guid.Empty)
            throw new ArgumentException("RequestedByHumanId is empty.", nameof(requestedByHumanId));

        Number = number.Trim();
        RequestedByHumanId = requestedByHumanId;
        RequestDate = requestDate;
        Status = PurchaseRequisitionStatus.Draft;
        Priority = RequisitionPriority.Normal;
    }

    public void SetRequiredByDate(DateOnly requiredByDate)
    {
        if (requiredByDate < RequestDate)
            throw new ArgumentOutOfRangeException(nameof(requiredByDate), "The required date cannot precede the request date.");
        RequiredByDate = requiredByDate;
    }

    public void SetPriority(RequisitionPriority priority) => Priority = priority;

    public void SetOrigin(Guid? departmentId, Guid? costCenterId, Guid? warehouseId, Guid? projectId)
    {
        DepartmentId = departmentId == Guid.Empty ? null : departmentId;
        CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;
        WarehouseId = warehouseId == Guid.Empty ? null : warehouseId;
        ProjectId = projectId == Guid.Empty ? null : projectId;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetJustification(string? justification) =>
        Justification = string.IsNullOrWhiteSpace(justification) ? null : justification.Trim();

    public void AddItem(PurchaseRequisitionItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureDraft();
        _items.Add(item);
    }

    public void RemoveItem(PurchaseRequisitionItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        EnsureDraft();
        _items.Remove(item);
    }

    public void Submit()
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("A requisition cannot be submitted without lines.");
        Status = PurchaseRequisitionStatus.Submitted;
    }

    public void Approve(Guid? approvalRequestId = null)
    {
        if (Status != PurchaseRequisitionStatus.Submitted)
            throw new InvalidOperationException($"Only a submitted requisition can be approved; this one is {Status}.");
        ApprovalRequestId = approvalRequestId == Guid.Empty ? null : approvalRequestId;
        Status = PurchaseRequisitionStatus.Approved;
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != PurchaseRequisitionStatus.Submitted)
            throw new InvalidOperationException($"Only a submitted requisition can be rejected; this one is {Status}.");
        Status = PurchaseRequisitionStatus.Rejected;
        RejectionReason = reason.Trim();
    }

    /// <summary>Recomputes how much of the requisition has made it onto purchase orders.</summary>
    public void RefreshFulfillmentStatus()
    {
        if (Status is not (PurchaseRequisitionStatus.Approved or PurchaseRequisitionStatus.PartiallyOrdered))
            return;
        Status = _items.All(i => i.IsFullyOrdered)
            ? PurchaseRequisitionStatus.Ordered
            : _items.Any(i => i.OrderedQuantity > 0m)
                ? PurchaseRequisitionStatus.PartiallyOrdered
                : PurchaseRequisitionStatus.Approved;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (_items.Any(i => i.OrderedQuantity > 0m))
            throw new InvalidOperationException("A requisition with orders against it cannot be cancelled.");
        Status = PurchaseRequisitionStatus.Cancelled;
        RejectionReason = reason.Trim();
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseRequisitionStatus.Draft)
            throw new InvalidOperationException($"The requisition is {Status} and can no longer be edited.");
    }
}
