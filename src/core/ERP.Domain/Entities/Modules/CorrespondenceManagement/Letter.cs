namespace ERP.Domain.Entities.Modules.CorrespondenceManagement;

/// <summary>
/// One piece of formal correspondence, in or out, with the registration number it was entered
/// under.
/// <para>
/// The registration number is the point of the whole module. In an Iranian office the number and
/// the date it was assigned are what make a letter citable - it is how a later letter refers back
/// to this one, and how an auditor finds it years afterwards. So it is assigned once, on
/// registration, and never reissued, even if the letter is later withdrawn.
/// </para>
/// <para>
/// The sender and recipient are held as free text alongside the optional identifiers, because most
/// correspondence is with organizations that will never be customers or suppliers in this system,
/// and a letter from a ministry must not require a record to be invented for it first.
/// </para>
/// </summary>
public sealed class Letter : BaseEntity, IBranchScoped
{
    /// <summary>The registration number, unique within the register it was entered in.</summary>
    public string Number { get; private set; } = default!;

    public LetterDirection Direction { get; private set; }
    public LetterStatus Status { get; private set; }
    public LetterConfidentiality Confidentiality { get; private set; }
    public LetterUrgency Urgency { get; private set; }

    public string Subject { get; private set; } = default!;
    public string? Body { get; private set; }

    /// <summary>The date written on the letter itself, which is rarely the date it was registered.</summary>
    public DateOnly LetterDate { get; private set; }
    public DateTimeOffset RegisteredOn { get; private set; }
    public string? RegisteredBy { get; private set; }

    public string CorrespondentName { get; private set; } = default!;

    /// <summary>The correspondent's own reference, quoted when replying.</summary>
    public string? CorrespondentReference { get; private set; }
    public DateOnly? CorrespondentReferenceDate { get; private set; }

    /// <summary>Filled in only when the correspondent happens to already exist in this system.</summary>
    public Guid? CustomerId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public Guid? HumanId { get; private set; }

    /// <summary>The letter this one answers.</summary>
    public Guid? InReplyToLetterId { get; private set; }

    /// <summary>An earlier letter in the same thread that this one continues.</summary>
    public Guid? FollowsLetterId { get; private set; }

    /// <summary>What the correspondence is about, when it concerns a document in this system.</summary>
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }

    public Guid? OwnerUserId { get; private set; }
    public Guid? BranchId { get; private set; }

    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset? ArchivedOn { get; private set; }
    public string? ArchiveLocation { get; private set; }
    public string? Description { get; private set; }

    private readonly List<LetterRecipient> _recipients = [];
    public IReadOnlyList<LetterRecipient> Recipients => _recipients;

    private readonly List<LetterReferral> _referrals = [];
    public IReadOnlyList<LetterReferral> Referrals => _referrals;

    private Letter() { }

    public Letter(string number, LetterDirection direction, string subject, string correspondentName, DateOnly letterDate, string? registeredBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(subject, nameof(subject));
        ArgumentException.ThrowIfNullOrWhiteSpace(correspondentName, nameof(correspondentName));

        Number = number.Trim();
        Direction = direction;
        Subject = subject.Trim();
        CorrespondentName = correspondentName.Trim();
        LetterDate = letterDate;
        RegisteredOn = DateTimeOffset.UtcNow;
        RegisteredBy = registeredBy;
        Status = direction == LetterDirection.Outbound ? LetterStatus.Draft : LetterStatus.Registered;
        Confidentiality = LetterConfidentiality.Normal;
        Urgency = LetterUrgency.Normal;
    }

    public void SetBody(string? body) => Body = string.IsNullOrWhiteSpace(body) ? null : body.Trim();

    public void SetHandling(LetterConfidentiality confidentiality, LetterUrgency urgency)
    {
        Confidentiality = confidentiality;
        Urgency = urgency;
    }

    public void SetCorrespondentReference(string? reference, DateOnly? referenceDate)
    {
        CorrespondentReference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        CorrespondentReferenceDate = referenceDate;
    }

    public void IdentifyCorrespondent(Guid? customerId, Guid? supplierId, Guid? humanId)
    {
        CustomerId = customerId == Guid.Empty ? null : customerId;
        SupplierId = supplierId == Guid.Empty ? null : supplierId;
        HumanId = humanId == Guid.Empty ? null : humanId;
    }

    public void SetThread(Guid? inReplyToLetterId, Guid? followsLetterId)
    {
        if (inReplyToLetterId == Id || followsLetterId == Id)
            throw new InvalidOperationException("A letter cannot be its own antecedent.");
        InReplyToLetterId = inReplyToLetterId == Guid.Empty ? null : inReplyToLetterId;
        FollowsLetterId = followsLetterId == Guid.Empty ? null : followsLetterId;
    }

    public void SetReference(string? referenceType, string? referenceId)
    {
        ReferenceType = string.IsNullOrWhiteSpace(referenceType) ? null : referenceType.Trim();
        ReferenceId = string.IsNullOrWhiteSpace(referenceId) ? null : referenceId.Trim();
    }

    public void SetDueDate(DateOnly? dueDate) => DueDate = dueDate;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AssignOwner(Guid userId) => OwnerUserId = userId == Guid.Empty ? null : userId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void AddRecipient(LetterRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient, nameof(recipient));
        EnsureNotArchived();
        _recipients.Add(recipient);
    }

    public void RemoveRecipient(LetterRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient, nameof(recipient));
        EnsureNotArchived();
        _recipients.Remove(recipient);
    }

    /// <summary>
    /// Passes the letter to somebody with an instruction, and records that it is now their move.
    /// This is the act the whole register exists to make traceable.
    /// </summary>
    public LetterReferral ReferTo(Guid toUserId, Guid fromUserId, string? instruction = null, DateOnly? dueDate = null)
    {
        EnsureNotArchived();
        if (toUserId == Guid.Empty)
            throw new ArgumentException("ToUserId is empty.", nameof(toUserId));
        if (_referrals.Any(r => r.ToUserId == toUserId && r.IsOutstanding))
            throw new InvalidOperationException("That person already has this letter open in their queue.");

        var referral = new LetterReferral(this, fromUserId, toUserId, instruction, dueDate);
        _referrals.Add(referral);

        if (Status is LetterStatus.Registered or LetterStatus.Sent)
            Status = LetterStatus.InProgress;

        Raise(new LetterReferred(Id, Number, toUserId, referral.Id));
        return referral;
    }

    /// <summary>Marks an outgoing letter as actually dispatched.</summary>
    public void MarkAsSent()
    {
        if (Direction != LetterDirection.Outbound)
            throw new InvalidOperationException("Only an outbound letter can be sent.");
        if (Status != LetterStatus.Draft)
            throw new InvalidOperationException($"A letter in state {Status} cannot be sent.");
        Status = LetterStatus.Sent;
    }

    public void Close()
    {
        EnsureNotArchived();
        if (_referrals.Any(r => r.IsOutstanding))
            throw new InvalidOperationException("Every referral must be answered or withdrawn before the letter is closed.");
        Status = LetterStatus.Closed;
    }

    /// <summary>
    /// Files the letter away. The number is kept: a withdrawn or archived letter still has to be
    /// findable by the reference somebody else quoted.
    /// </summary>
    public void Archive(string? archiveLocation = null)
    {
        if (Status == LetterStatus.Archived)
            return;
        Status = LetterStatus.Archived;
        ArchivedOn = DateTimeOffset.UtcNow;
        ArchiveLocation = string.IsNullOrWhiteSpace(archiveLocation) ? null : archiveLocation.Trim();
    }

    public bool IsOverdue(DateOnly asOf) =>
        DueDate.HasValue && Status is not (LetterStatus.Closed or LetterStatus.Archived) && DueDate.Value < asOf;

    public bool IsOpen => Status is LetterStatus.Registered or LetterStatus.InProgress or LetterStatus.Sent or LetterStatus.Draft;

    public IReadOnlyList<LetterReferral> OutstandingReferrals => [.. _referrals.Where(r => r.IsOutstanding)];

    private void EnsureNotArchived()
    {
        if (Status == LetterStatus.Archived)
            throw new InvalidOperationException("An archived letter can no longer be changed.");
    }
}
