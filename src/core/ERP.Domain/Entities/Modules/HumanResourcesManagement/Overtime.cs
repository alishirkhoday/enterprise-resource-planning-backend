namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Hours worked beyond the contracted schedule. Payroll multiplies them by the rate that
/// applies to their <see cref="Type"/>.
/// </summary>
public sealed class Overtime : BaseEntity
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public DateOnly Date { get; private set; }
    public OvertimeType Type { get; private set; }
    public decimal Hours { get; private set; }

    /// <summary>Multiplier applied to the hourly wage, for example 1.4 for ordinary overtime.</summary>
    public decimal RateMultiplier { get; private set; }

    public HumanRequestStatus Status { get; private set; }
    public Guid? ApprovedByHumanId { get; private set; }
    public DateTimeOffset? ApprovedOn { get; private set; }
    public string? Reason { get; private set; }
    public Guid? CostCenterId { get; private set; }

    private Overtime() { }

    public Overtime(Human human, DateOnly date, OvertimeType type, decimal hours, decimal rateMultiplier)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hours, nameof(hours));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hours, 24m, nameof(hours));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rateMultiplier, nameof(rateMultiplier));

        Human = human;
        HumanId = human.Id;
        Date = date;
        Type = type;
        Hours = hours;
        RateMultiplier = rateMultiplier;
        Status = HumanRequestStatus.Pending;
    }

    public void SetReason(string? reason) => Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void Approve(Guid approvedByHumanId)
    {
        EnsurePending();
        Status = HumanRequestStatus.Approved;
        ApprovedByHumanId = approvedByHumanId;
        ApprovedOn = DateTimeOffset.UtcNow;
    }

    public void Reject(Guid rejectedByHumanId)
    {
        EnsurePending();
        Status = HumanRequestStatus.Rejected;
        ApprovedByHumanId = rejectedByHumanId;
        ApprovedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>The payable amount for these hours given the employee hourly wage.</summary>
    public Money CalculatePay(Money hourlyWage)
    {
        ArgumentNullException.ThrowIfNull(hourlyWage, nameof(hourlyWage));
        return hourlyWage.Multiply(Hours * RateMultiplier);
    }

    private void EnsurePending()
    {
        if (Status != HumanRequestStatus.Pending)
            throw new InvalidOperationException($"The overtime record is already {Status}.");
    }
}
