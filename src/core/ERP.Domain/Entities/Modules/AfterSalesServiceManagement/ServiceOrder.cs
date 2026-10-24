using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

/// <summary>
/// The visit itself: somebody going out to fix something, and what it took.
/// <para>
/// Why it is being done is decided once, on creation, and everything downstream follows from it. A
/// warranty visit consumes margin already earned, a contract visit draws down an allowance, and a
/// chargeable visit becomes an invoice. Deciding that at the end, after the parts are fitted, is
/// how service revenue quietly disappears.
/// </para>
/// </summary>
public sealed partial class ServiceOrder : AggregateRoot, IBranchScoped
{
    public string Number { get; private set; } = default!;

    public Customer Customer { get; private set; } = default!;
    public Guid CustomerId { get; private set; }

    public ServiceOrderType Type { get; private set; }
    public ServiceOrderStatus Status { get; private set; }
    public ServiceOrderPriority Priority { get; private set; }

    public Guid? ItemId { get; private set; }
    public Guid? SerialNumberId { get; private set; }
    public Guid? ServiceContractId { get; private set; }
    public Guid? WarrantyClaimId { get; private set; }
    public Guid? TicketId { get; private set; }

    public string ReportedFault { get; private set; } = default!;
    public string? DiagnosisNotes { get; private set; }
    public string? WorkPerformed { get; private set; }

    public Address? ServiceLocation { get; private set; }

    public Guid? AssignedToHumanId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? WarehouseId { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? ResponseDueAt { get; private set; }
    public DateTimeOffset? ResolutionDueAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }
    public DateOnly? ScheduledOn { get; private set; }
    public DateOnly? CompletedOn { get; private set; }

    public decimal LabourHours { get; private set; }
    public string CurrencyCode { get; private set; } = default!;
    public Money LabourCharge { get; private set; } = default!;
    public Money TravelCharge { get; private set; } = default!;

    /// <summary>Set once the visit has been billed, which is what stops it being billed again.</summary>
    public Guid? InvoiceId { get; private set; }

    public string? CancellationReason { get; private set; }

    private readonly List<ServiceOrderPart> _parts = [];
    public IReadOnlyList<ServiceOrderPart> Parts => _parts.AsReadOnly();

    private ServiceOrder() { }

    public ServiceOrder(string number, Customer customer, ServiceOrderType type, string reportedFault, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(reportedFault, nameof(reportedFault));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));

        Number = number.Trim();
        Customer = customer;
        CustomerId = customer.Id;
        Type = type;
        ReportedFault = reportedFault.Trim();
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        LabourCharge = Money.Zero(CurrencyCode);
        TravelCharge = Money.Zero(CurrencyCode);
        RequestedAt = DateTimeOffset.UtcNow;
        Status = ServiceOrderStatus.Open;
        Priority = ServiceOrderPriority.Normal;
    }

    public void IdentifyEquipment(Guid? itemId, Guid? serialNumberId)
    {
        ItemId = itemId == Guid.Empty ? null : itemId;
        SerialNumberId = serialNumberId == Guid.Empty ? null : serialNumberId;
    }

    public void LinkToServiceContract(Guid serviceContractId) =>
        ServiceContractId = serviceContractId == Guid.Empty ? null : serviceContractId;

    public void LinkToWarrantyClaim(Guid warrantyClaimId) =>
        WarrantyClaimId = warrantyClaimId == Guid.Empty ? null : warrantyClaimId;

    public void LinkToTicket(Guid ticketId) => TicketId = ticketId == Guid.Empty ? null : ticketId;

    public void SetPriority(ServiceOrderPriority priority) => Priority = priority;

    /// <summary>
    /// Records what was promised. Held as moments rather than durations so a breach is a comparison
    /// the database can make, without knowing anything about working hours.
    /// </summary>
    public void PromiseServiceLevel(int? responseHours, int? resolutionHours)
    {
        ResponseDueAt = responseHours.HasValue ? RequestedAt.AddHours(responseHours.Value) : null;
        ResolutionDueAt = resolutionHours.HasValue ? RequestedAt.AddHours(resolutionHours.Value) : null;
    }

    public void SetServiceLocation(Address serviceLocation)
    {
        ArgumentNullException.ThrowIfNull(serviceLocation, nameof(serviceLocation));
        ServiceLocation = serviceLocation;
    }

    public void AssignTo(Guid humanId)
    {
        EnsureOpen();
        AssignedToHumanId = humanId == Guid.Empty ? null : humanId;
        if (AssignedToHumanId is not null && Status == ServiceOrderStatus.Open)
            Status = ServiceOrderStatus.Assigned;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void IssuePartsFrom(Guid warehouseId) => WarehouseId = warehouseId == Guid.Empty ? null : warehouseId;

    public void Schedule(DateOnly scheduledOn)
    {
        EnsureOpen();
        ScheduledOn = scheduledOn;
        Status = ServiceOrderStatus.Scheduled;
    }

    /// <summary>First contact with the customer, which is what the response promise is measured against.</summary>
    public void RecordFirstResponse()
    {
        EnsureOpen();
        RespondedAt ??= DateTimeOffset.UtcNow;
    }

    public void Start(string? diagnosisNotes = null)
    {
        EnsureOpen();
        RecordFirstResponse();
        DiagnosisNotes = string.IsNullOrWhiteSpace(diagnosisNotes) ? DiagnosisNotes : diagnosisNotes.Trim();
        Status = ServiceOrderStatus.InProgress;
    }

    public void AddPart(ServiceOrderPart part)
    {
        ArgumentNullException.ThrowIfNull(part);
        EnsureCanAddChild(part, part.ServiceOrderId, _parts);

        EnsureOpen();
        if (part.UnitPrice.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Parts on this order must be priced in {CurrencyCode}.");
        _parts.Add(part);
    }

    public void RemovePart(ServiceOrderPart part)
    {
        ArgumentNullException.ThrowIfNull(part);
        if (part.ServiceOrderId != Id || !_parts.Contains(part))
            throw new InvalidOperationException("The child is not attached to this owner.");

        ArgumentNullException.ThrowIfNull(part, nameof(part));
        EnsureOpen();
        _parts.Remove(part);
    }

    public void RecordLabour(decimal labourHours, Money labourCharge, Money travelCharge)
    {
        ArgumentNullException.ThrowIfNull(labourCharge, nameof(labourCharge));
        ArgumentNullException.ThrowIfNull(travelCharge, nameof(travelCharge));
        ArgumentOutOfRangeException.ThrowIfNegative(labourHours, nameof(labourHours));
        EnsureOpen();
        if (labourCharge.CurrencyCode != CurrencyCode || travelCharge.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"Charges on this order must be expressed in {CurrencyCode}.", nameof(labourCharge));

        LabourHours = labourHours;
        LabourCharge = labourCharge;
        TravelCharge = travelCharge;
    }

    /// <summary>Puts the visit on hold, normally because a part has to come in first.</summary>
    public void AwaitParts()
    {
        if (Status is not (ServiceOrderStatus.InProgress or ServiceOrderStatus.Scheduled))
            throw new InvalidOperationException($"An order in state {Status} cannot be put on hold.");
        Status = ServiceOrderStatus.AwaitingParts;
    }

    public void Complete(DateOnly completedOn, string workPerformed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workPerformed, nameof(workPerformed));
        EnsureOpen();

        WorkPerformed = workPerformed.Trim();
        CompletedOn = completedOn;
        Status = ServiceOrderStatus.Completed;
        RespondedAt ??= DateTimeOffset.UtcNow;
        Raise(new ServiceOrderCompleted(Id, Number, CustomerId, completedOn));
    }

    public void MarkAsInvoiced(Guid invoiceId)
    {
        if (Status != ServiceOrderStatus.Completed)
            throw new InvalidOperationException("Only a completed service order can be invoiced.");
        if (!IsChargeable)
            throw new InvalidOperationException($"A {Type} service order is not chargeable to the customer.");
        if (InvoiceId is not null)
            throw new InvalidOperationException("This service order has already been invoiced.");
        InvoiceId = invoiceId;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status is ServiceOrderStatus.Completed or ServiceOrderStatus.Cancelled)
            throw new InvalidOperationException($"An order in state {Status} cannot be cancelled.");
        Status = ServiceOrderStatus.Cancelled;
        CancellationReason = reason.Trim();
    }

    public Money PartsTotal() =>
        _parts.Count == 0
            ? Money.Zero(CurrencyCode)
            : _parts.Select(p => p.LineTotal).Aggregate((left, right) => left.Add(right));

    public Money GrandTotal() => PartsTotal().Add(LabourCharge).Add(TravelCharge);

    /// <summary>
    /// Whether the customer pays. Warranty and contract work is already paid for, so only a
    /// chargeable visit may reach an invoice.
    /// </summary>
    public bool IsChargeable => Type == ServiceOrderType.Chargeable;

    public bool IsOpen => Status is ServiceOrderStatus.Open or ServiceOrderStatus.Assigned
        or ServiceOrderStatus.Scheduled or ServiceOrderStatus.InProgress or ServiceOrderStatus.AwaitingParts;

    public bool HasBreachedResponse(DateTimeOffset asOf) =>
        ResponseDueAt is not null && RespondedAt is null && ResponseDueAt < asOf;

    public bool HasBreachedResolution(DateTimeOffset asOf) =>
        ResolutionDueAt is not null && CompletedOn is null && IsOpen && ResolutionDueAt < asOf;

    private void EnsureOpen()
    {
        if (!IsOpen)
            throw new InvalidOperationException($"A service order in state {Status} can no longer be worked on.");
    }
}
