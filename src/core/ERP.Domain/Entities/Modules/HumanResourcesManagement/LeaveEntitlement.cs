namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// How much leave of one kind an employee has for one year, and how much of it is already gone.
/// Without this a leave request cannot be validated against a balance.
/// </summary>
public sealed class LeaveEntitlement : AggregateRoot
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public HumanRequestLeaveType LeaveType { get; private set; }

    /// <summary>The year the entitlement belongs to.</summary>
    public int Year { get; private set; }

    /// <summary>Days granted for the year by policy.</summary>
    public decimal EntitledDays { get; private set; }

    /// <summary>Days carried over from the previous year.</summary>
    public decimal CarriedOverDays { get; private set; }

    /// <summary>Days already approved and taken.</summary>
    public decimal UsedDays { get; private set; }

    /// <summary>Days requested but not yet approved, held so they cannot be double spent.</summary>
    public decimal PendingDays { get; private set; }

    public decimal TotalDays => EntitledDays + CarriedOverDays;
    public decimal RemainingDays => TotalDays - UsedDays - PendingDays;

    private LeaveEntitlement() { }

    public LeaveEntitlement(Human human, HumanRequestLeaveType leaveType, int year, decimal entitledDays)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentOutOfRangeException.ThrowIfNegative(entitledDays, nameof(entitledDays));
        if (year < 1300 || year > 2200)
            throw new ArgumentOutOfRangeException(nameof(year), "Year is out of range.");
        Human = human;
        HumanId = human.Id;
        LeaveType = leaveType;
        Year = year;
        EntitledDays = entitledDays;
    }

    public void SetEntitledDays(decimal days)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(days, nameof(days));
        if (days < UsedDays + PendingDays)
            throw new InvalidOperationException("Entitled days cannot be lower than what is already used or pending.");
        EntitledDays = days;
    }

    public void CarryOver(decimal days)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(days, nameof(days));
        CarriedOverDays = days;
    }

    /// <summary>Holds days for a request that has not been decided yet.</summary>
    public void Reserve(decimal days)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days, nameof(days));
        if (days > RemainingDays)
            throw new InvalidOperationException($"Insufficient leave balance. Remaining is {RemainingDays} days.");
        PendingDays += days;
    }

    /// <summary>Turns held days into used days once the request is approved.</summary>
    public void Consume(decimal days)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days, nameof(days));
        if (days > PendingDays)
            throw new InvalidOperationException("Cannot consume more days than are currently pending.");
        PendingDays -= days;
        UsedDays += days;
    }

    /// <summary>Gives held days back when the request is rejected or withdrawn.</summary>
    public void Release(decimal days)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days, nameof(days));
        if (days > PendingDays)
            throw new InvalidOperationException("Cannot release more days than are currently pending.");
        PendingDays -= days;
    }

    /// <summary>Returns used days when an approved leave is cancelled.</summary>
    public void Refund(decimal days)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days, nameof(days));
        if (days > UsedDays)
            throw new InvalidOperationException("Cannot refund more days than are currently used.");
        UsedDays -= days;
    }
}
