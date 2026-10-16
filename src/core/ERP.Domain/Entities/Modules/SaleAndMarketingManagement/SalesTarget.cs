namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// A sales quota for a period, set against a person, a team or a territory, and tracked against
/// what was actually sold.
/// </summary>
public sealed class SalesTarget : BaseEntity, IBranchScoped
{
    public string Title { get; private set; } = default!;
    public SalesTargetScope Scope { get; private set; }

    /// <summary>Identifier of the person, team or territory the target belongs to.</summary>
    public Guid ScopeReferenceId { get; private set; }

    public DateRange Period { get; private set; } = default!;
    public Money TargetAmount { get; private set; } = default!;
    public Money AchievedAmount { get; private set; } = default!;

    /// <summary>An optional count target alongside the value, such as new customers won.</summary>
    public int? TargetCount { get; private set; }
    public int AchievedCount { get; private set; }

    public Guid? ItemGroupId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string? Description { get; private set; }

    private SalesTarget() { }

    public SalesTarget(string title, SalesTargetScope scope, Guid scopeReferenceId, DateRange period, Money targetAmount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        ArgumentNullException.ThrowIfNull(targetAmount, nameof(targetAmount));
        if (scopeReferenceId == Guid.Empty)
            throw new ArgumentException("ScopeReferenceId is empty.", nameof(scopeReferenceId));
        if (!targetAmount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(targetAmount), "A target must be a positive amount.");

        Title = title.Trim();
        Scope = scope;
        ScopeReferenceId = scopeReferenceId;
        Period = period;
        TargetAmount = targetAmount;
        AchievedAmount = Money.Zero(targetAmount.CurrencyCode);
    }

    public void SetCountTarget(int targetCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetCount, nameof(targetCount));
        TargetCount = targetCount;
    }

    public void NarrowToItemGroup(Guid itemGroupId) => ItemGroupId = itemGroupId == Guid.Empty ? null : itemGroupId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void RecordAchievement(Money amount, int count = 0)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        ArgumentOutOfRangeException.ThrowIfNegative(count, nameof(count));
        AchievedAmount = AchievedAmount.Add(amount);
        AchievedCount += count;
    }

    public void Reset()
    {
        AchievedAmount = Money.Zero(TargetAmount.CurrencyCode);
        AchievedCount = 0;
    }

    public decimal AchievementPercentage =>
        TargetAmount.IsZero ? 0m : AchievedAmount.Amount / TargetAmount.Amount * 100m;

    public Money Shortfall => AchievedAmount.Amount >= TargetAmount.Amount
        ? Money.Zero(TargetAmount.CurrencyCode)
        : TargetAmount.Subtract(AchievedAmount);

    public bool IsMet => AchievedAmount.Amount >= TargetAmount.Amount;
}
