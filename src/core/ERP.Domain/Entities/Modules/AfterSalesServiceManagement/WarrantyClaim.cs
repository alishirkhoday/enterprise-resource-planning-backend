using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

/// <summary>
/// A customer saying something broke and that it should not have. Whether they are right is a
/// decision this entity records rather than assumes.
/// <para>
/// The date the fault was reported is kept apart from the date it was registered. Cover is judged
/// on when the thing failed, not on how long the paperwork took, and a claim raised a day after
/// expiry for a failure a week before it is still a valid claim.
/// </para>
/// </summary>
public sealed class WarrantyClaim : BaseEntity, IBranchScoped
{
    public string Number { get; private set; } = default!;

    public Customer Customer { get; private set; } = default!;
    public Guid CustomerId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    /// <summary>The unit that failed, when the item is serialised.</summary>
    public Guid? SerialNumberId { get; private set; }
    public Guid? ItemBatchId { get; private set; }

    /// <summary>The sale being claimed against, which is what proves entitlement.</summary>
    public Guid? InvoiceId { get; private set; }
    public Guid? ServiceContractId { get; private set; }

    /// <summary>When the customer says it failed.</summary>
    public DateOnly FaultOccurredOn { get; private set; }

    /// <summary>When this organization heard about it.</summary>
    public DateOnly ReportedOn { get; private set; }

    public WarrantyClaimStatus Status { get; private set; }
    public WarrantyClaimResolution Resolution { get; private set; }

    public string FaultDescription { get; private set; } = default!;
    public string? InspectionFindings { get; private set; }
    public string? RejectionReason { get; private set; }

    /// <summary>What honouring the claim cost, for the quality figures rather than for billing.</summary>
    public Money? ClaimCost { get; private set; }

    public Guid? ServiceOrderId { get; private set; }
    public Guid? ReplacementOrderId { get; private set; }
    public Guid? CreditNoteInvoiceId { get; private set; }

    /// <summary>Set when the cost is being passed back to whoever supplied the faulty part.</summary>
    public Guid? SupplierId { get; private set; }
    public string? SupplierClaimReference { get; private set; }

    public Guid? AssignedToHumanId { get; private set; }
    public Guid? BranchId { get; private set; }

    public DateOnly? AssessedOn { get; private set; }
    public DateOnly? ClosedOn { get; private set; }

    private WarrantyClaim() { }

    public WarrantyClaim(string number, Customer customer, Item item, DateOnly faultOccurredOn, DateOnly reportedOn, string faultDescription)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(faultDescription, nameof(faultDescription));
        if (reportedOn < faultOccurredOn)
            throw new ArgumentException("A fault cannot be reported before it happened.", nameof(reportedOn));

        Number = number.Trim();
        Customer = customer;
        CustomerId = customer.Id;
        Item = item;
        ItemId = item.Id;
        FaultOccurredOn = faultOccurredOn;
        ReportedOn = reportedOn;
        FaultDescription = faultDescription.Trim();
        Status = WarrantyClaimStatus.Registered;
        Resolution = WarrantyClaimResolution.Pending;
    }

    public void IdentifyUnit(Guid? serialNumberId, Guid? itemBatchId)
    {
        SerialNumberId = serialNumberId == Guid.Empty ? null : serialNumberId;
        ItemBatchId = itemBatchId == Guid.Empty ? null : itemBatchId;
    }

    public void LinkToSale(Guid invoiceId) => InvoiceId = invoiceId == Guid.Empty ? null : invoiceId;

    public void LinkToServiceContract(Guid serviceContractId) =>
        ServiceContractId = serviceContractId == Guid.Empty ? null : serviceContractId;

    public void AssignTo(Guid humanId) => AssignedToHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void BeginAssessment()
    {
        if (Status != WarrantyClaimStatus.Registered)
            throw new InvalidOperationException($"A claim in state {Status} is not waiting for assessment.");
        Status = WarrantyClaimStatus.UnderAssessment;
    }

    /// <summary>
    /// Accepts the claim and says how it will be honoured. The resolution is decided here rather
    /// than on closing, because the work that follows depends on which one it is.
    /// </summary>
    public void Approve(DateOnly assessedOn, WarrantyClaimResolution resolution, string? findings = null)
    {
        EnsureOpen();
        if (resolution == WarrantyClaimResolution.Pending)
            throw new ArgumentException("An approved claim needs a decided resolution.", nameof(resolution));

        Status = WarrantyClaimStatus.Approved;
        Resolution = resolution;
        AssessedOn = assessedOn;
        InspectionFindings = string.IsNullOrWhiteSpace(findings) ? InspectionFindings : findings.Trim();
        RejectionReason = null;
    }

    public void Reject(DateOnly assessedOn, string reason, string? findings = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        EnsureOpen();

        Status = WarrantyClaimStatus.Rejected;
        Resolution = WarrantyClaimResolution.Declined;
        AssessedOn = assessedOn;
        RejectionReason = reason.Trim();
        InspectionFindings = string.IsNullOrWhiteSpace(findings) ? InspectionFindings : findings.Trim();
    }

    public void RecordFulfilment(Guid? serviceOrderId = null, Guid? replacementOrderId = null, Guid? creditNoteInvoiceId = null)
    {
        if (Status != WarrantyClaimStatus.Approved)
            throw new InvalidOperationException("Only an approved claim can be fulfilled.");
        ServiceOrderId = serviceOrderId == Guid.Empty ? null : serviceOrderId;
        ReplacementOrderId = replacementOrderId == Guid.Empty ? null : replacementOrderId;
        CreditNoteInvoiceId = creditNoteInvoiceId == Guid.Empty ? null : creditNoteInvoiceId;
    }

    public void RecordCost(Money claimCost)
    {
        ArgumentNullException.ThrowIfNull(claimCost, nameof(claimCost));
        if (claimCost.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(claimCost), "A claim cost cannot be negative.");
        ClaimCost = claimCost;
    }

    /// <summary>Passes the cost back up the chain to whoever supplied the faulty part.</summary>
    public void RecoverFromSupplier(Guid supplierId, string supplierClaimReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierClaimReference, nameof(supplierClaimReference));
        if (supplierId == Guid.Empty)
            throw new ArgumentException("SupplierId is empty.", nameof(supplierId));
        SupplierId = supplierId;
        SupplierClaimReference = supplierClaimReference.Trim();
    }

    public void Close(DateOnly closedOn)
    {
        if (Status is not (WarrantyClaimStatus.Approved or WarrantyClaimStatus.Rejected))
            throw new InvalidOperationException($"A claim in state {Status} cannot be closed.");
        Status = WarrantyClaimStatus.Closed;
        ClosedOn = closedOn;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == WarrantyClaimStatus.Closed)
            throw new InvalidOperationException("A closed claim cannot be cancelled.");
        Status = WarrantyClaimStatus.Cancelled;
        RejectionReason = reason.Trim();
    }

    public bool IsOpen => Status is WarrantyClaimStatus.Registered or WarrantyClaimStatus.UnderAssessment or WarrantyClaimStatus.Approved;

    /// <summary>Days the customer has been waiting, which is the number anybody actually cares about.</summary>
    public int AgeInDays(DateOnly asOf) => (ClosedOn ?? asOf).DayNumber - ReportedOn.DayNumber;

    private void EnsureOpen()
    {
        if (Status is not (WarrantyClaimStatus.Registered or WarrantyClaimStatus.UnderAssessment))
            throw new InvalidOperationException($"A claim in state {Status} has already been decided.");
    }
}
