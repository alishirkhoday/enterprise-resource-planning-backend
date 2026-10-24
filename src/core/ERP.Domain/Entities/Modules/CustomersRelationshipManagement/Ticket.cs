namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// A customer issue and the conversation around it, measured against the response and resolution
/// times the organization has promised.
/// </summary>
public sealed class Ticket : AggregateRoot, IBranchScoped
{
    public string Number { get; private set; } = default!;

    public Customer Customer { get; private set; } = default!;
    public Guid CustomerId { get; private set; }

    public TicketSubject Subject { get; private set; } = default!;
    public Guid SubjectId { get; private set; }

    public string Title { get; private set; } = default!;
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public TicketChannel Channel { get; private set; }

    public Guid? AssignedToHumanId { get; private set; }
    public Guid? BranchId { get; private set; }

    /// <summary>What the ticket is about, such as an order or an invoice.</summary>
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }

    public DateTimeOffset? FirstRespondedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>When a first response is promised by.</summary>
    public DateTimeOffset? ResponseDueAt { get; private set; }

    /// <summary>When a resolution is promised by.</summary>
    public DateTimeOffset? ResolutionDueAt { get; private set; }

    /// <summary>How the customer rated the handling, out of five.</summary>
    public int? SatisfactionRating { get; private set; }

    public string? Resolution { get; private set; }

    private readonly List<TicketMessage> _messages = [];
    public IReadOnlyList<TicketMessage> Messages => _messages.AsReadOnly();

    private Ticket() { }

    public Ticket(string number, Customer customer, TicketSubject subject, string title, TicketChannel channel = TicketChannel.Portal)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        ArgumentNullException.ThrowIfNull(subject, nameof(subject));
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        Number = number.Trim();
        Customer = customer;
        CustomerId = customer.Id;
        Subject = subject;
        SubjectId = subject.Id;
        Title = title.Trim();
        Channel = channel;
        Priority = TicketPriority.Normal;
        Status = TicketStatus.Open;
    }

    public void SetPriority(TicketPriority priority)
    {
        EnsureOpen();
        Priority = priority;
    }

    /// <summary>Applies the promised response and resolution times to this ticket.</summary>
    public void ApplyServiceLevel(int responseMinutes, int resolutionMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(responseMinutes, nameof(responseMinutes));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(resolutionMinutes, nameof(resolutionMinutes));
        ResponseDueAt = CreationDateTime.AddMinutes(responseMinutes);
        ResolutionDueAt = CreationDateTime.AddMinutes(resolutionMinutes);
    }

    public void AssignTo(Guid humanId)
    {
        EnsureOpen();
        AssignedToHumanId = humanId == Guid.Empty ? null : humanId;
        if (Status == TicketStatus.Open)
            Status = TicketStatus.InProgress;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetReference(string referenceType, string referenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
    }

    public void AddMessage(TicketMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        EnsureCanAddChild(message, message.TicketId, _messages);

        if (Status == TicketStatus.Closed)
            throw new InvalidOperationException("A closed ticket cannot take new messages; reopen it first.");

        _messages.Add(message);

        if (message.CreatorType != TicketMessageCreatorType.Customer && FirstRespondedAt is null)
            FirstRespondedAt = DateTimeOffset.UtcNow;

        if (message.CreatorType == TicketMessageCreatorType.Customer && Status == TicketStatus.AwaitingCustomer)
            Status = TicketStatus.InProgress;
    }

    public void AwaitCustomer()
    {
        EnsureOpen();
        Status = TicketStatus.AwaitingCustomer;
    }

    public void Resolve(string resolution)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resolution, nameof(resolution));
        EnsureOpen();
        Resolution = resolution.Trim();
        ResolvedAt = DateTimeOffset.UtcNow;
        Status = TicketStatus.Resolved;
    }

    public void Close()
    {
        if (Status != TicketStatus.Resolved)
            throw new InvalidOperationException("Only a resolved ticket can be closed.");
        ClosedAt = DateTimeOffset.UtcNow;
        Status = TicketStatus.Closed;
    }

    public void Reopen(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status is not (TicketStatus.Resolved or TicketStatus.Closed))
            throw new InvalidOperationException("Only a resolved or closed ticket can be reopened.");
        Status = TicketStatus.InProgress;
        ResolvedAt = null;
        ClosedAt = null;
    }

    public void RecordSatisfaction(int rating)
    {
        if (Status != TicketStatus.Closed)
            throw new InvalidOperationException("Satisfaction is recorded once the ticket is closed.");
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "A rating must be between 1 and 5.");
        SatisfactionRating = rating;
    }

    public bool HasBreachedResponseTarget =>
        ResponseDueAt.HasValue && (FirstRespondedAt ?? DateTimeOffset.UtcNow) > ResponseDueAt.Value;

    public bool HasBreachedResolutionTarget =>
        ResolutionDueAt.HasValue && (ResolvedAt ?? DateTimeOffset.UtcNow) > ResolutionDueAt.Value;

    private void EnsureOpen()
    {
        if (Status == TicketStatus.Closed)
            throw new InvalidOperationException("The ticket is closed.");
    }
}
