namespace ERP.Domain.Entities.Modules.CorrespondenceManagement;

/// <summary>
/// One hand-off of a letter: who passed it to whom, what they asked for, and what came back.
/// <para>
/// Referrals are kept rather than overwritten. The question anyone asks about a letter that went
/// nowhere is where it stopped, and that is only answerable if each step survives the next one.
/// </para>
/// </summary>
public sealed class LetterReferral : BaseEntity
{
    public Letter Letter { get; private set; } = default!;
    public Guid LetterId { get; private set; }

    public Guid FromUserId { get; private set; }
    public Guid ToUserId { get; private set; }

    /// <summary>What the sender asked for - the note written across the top of the letter.</summary>
    public string? Instruction { get; private set; }

    public LetterReferralStatus Status { get; private set; }
    public DateTimeOffset ReferredOn { get; private set; }
    public DateOnly? DueDate { get; private set; }

    public DateTimeOffset? SeenOn { get; private set; }
    public DateTimeOffset? RespondedOn { get; private set; }
    public string? Response { get; private set; }

    private LetterReferral() { }

    public LetterReferral(Letter letter, Guid fromUserId, Guid toUserId, string? instruction = null, DateOnly? dueDate = null)
    {
        ArgumentNullException.ThrowIfNull(letter, nameof(letter));
        if (toUserId == Guid.Empty)
            throw new ArgumentException("ToUserId is empty.", nameof(toUserId));
        if (toUserId == fromUserId)
            throw new ArgumentException("A letter cannot be referred to the person referring it.", nameof(toUserId));

        Letter = letter;
        LetterId = letter.Id;
        FromUserId = fromUserId;
        ToUserId = toUserId;
        Instruction = string.IsNullOrWhiteSpace(instruction) ? null : instruction.Trim();
        DueDate = dueDate;
        ReferredOn = DateTimeOffset.UtcNow;
        Status = LetterReferralStatus.Pending;
    }

    /// <summary>
    /// Records that the recipient opened it. Kept apart from answering, because "it was never
    /// passed to me" and "I have not got to it yet" are different problems.
    /// </summary>
    public void MarkAsSeen()
    {
        if (Status != LetterReferralStatus.Pending)
            return;
        SeenOn ??= DateTimeOffset.UtcNow;
        Status = LetterReferralStatus.Seen;
    }

    public void Respond(string response)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(response, nameof(response));
        if (!IsOutstanding)
            throw new InvalidOperationException($"A referral in state {Status} has already been dealt with.");

        Response = response.Trim();
        RespondedOn = DateTimeOffset.UtcNow;
        SeenOn ??= RespondedOn;
        Status = LetterReferralStatus.Answered;
    }

    /// <summary>Takes the letter back out of somebody's queue without them answering it.</summary>
    public void Withdraw(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (!IsOutstanding)
            throw new InvalidOperationException($"A referral in state {Status} cannot be withdrawn.");
        Response = reason.Trim();
        RespondedOn = DateTimeOffset.UtcNow;
        Status = LetterReferralStatus.Withdrawn;
    }

    public bool IsOutstanding => Status is LetterReferralStatus.Pending or LetterReferralStatus.Seen;

    public bool IsOverdue(DateOnly asOf) => IsOutstanding && DueDate.HasValue && DueDate.Value < asOf;

    /// <summary>How long it sat unopened, which is the number that shows where a register is blocked.</summary>
    public TimeSpan? TimeToFirstView => SeenOn - ReferredOn;
}
