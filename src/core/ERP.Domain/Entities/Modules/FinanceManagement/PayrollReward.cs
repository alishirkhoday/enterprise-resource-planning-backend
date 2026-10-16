namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Something added to somebody's pay for a period - مزایا. Kept as rows rather than a single
/// figure so a payslip can explain itself.
/// </summary>
public sealed class PayrollReward : BaseEntity
{
    public Payroll Payroll { get; private set; } = default!;
    public Guid PayrollId { get; private set; }

    public Reward Reward { get; private set; } = default!;
    public Guid RewardId { get; private set; }

    /// <summary>The amount actually granted, which may differ from the reward default.</summary>
    public Money AppliedAmount { get; private set; } = default!;

    public string? Description { get; private set; }

    private PayrollReward() { }

    public PayrollReward(Payroll payroll, Reward reward, Money appliedAmount)
    {
        ArgumentNullException.ThrowIfNull(payroll, nameof(payroll));
        ArgumentNullException.ThrowIfNull(reward, nameof(reward));
        ArgumentNullException.ThrowIfNull(appliedAmount, nameof(appliedAmount));
        if (appliedAmount.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(appliedAmount), "A granted reward cannot be negative.");
        if (appliedAmount.CurrencyCode != payroll.CurrencyCode)
            throw new ArgumentException($"The reward must be expressed in {payroll.CurrencyCode}.", nameof(appliedAmount));

        Payroll = payroll;
        PayrollId = payroll.Id;
        Reward = reward;
        RewardId = reward.Id;
        AppliedAmount = appliedAmount;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
