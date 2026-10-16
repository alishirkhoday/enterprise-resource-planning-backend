namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// What one account is allowed in one period, and what it has actually taken so far.
/// </summary>
public sealed class BudgetLine : BaseEntity
{
    public Budget Budget { get; private set; } = default!;
    public Guid BudgetId { get; private set; }

    public Account Account { get; private set; } = default!;
    public Guid AccountId { get; private set; }

    /// <summary>Which period of the year this line covers. Zero means the whole year.</summary>
    public short PeriodNumber { get; private set; }

    public Money PlannedAmount { get; private set; } = default!;
    public Money ActualAmount { get; private set; } = default!;

    /// <summary>Amounts committed but not yet spent, such as an open purchase order.</summary>
    public Money CommittedAmount { get; private set; } = default!;

    public Money Variance => PlannedAmount.Subtract(ActualAmount);
    public Money Available => PlannedAmount.Subtract(ActualAmount).Subtract(CommittedAmount);

    public Guid? CostCenterId { get; private set; }
    public string? Description { get; private set; }

    private BudgetLine() { }

    public BudgetLine(Budget budget, Account account, short periodNumber, Money plannedAmount)
    {
        ArgumentNullException.ThrowIfNull(budget, nameof(budget));
        ArgumentNullException.ThrowIfNull(account, nameof(account));
        ArgumentNullException.ThrowIfNull(plannedAmount, nameof(plannedAmount));
        ArgumentOutOfRangeException.ThrowIfNegative(periodNumber, nameof(periodNumber));
        if (plannedAmount.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(plannedAmount), "A planned amount cannot be negative.");

        Budget = budget;
        BudgetId = budget.Id;
        Account = account;
        AccountId = account.Id;
        PeriodNumber = periodNumber;
        PlannedAmount = plannedAmount;
        ActualAmount = Money.Zero(plannedAmount.CurrencyCode);
        CommittedAmount = Money.Zero(plannedAmount.CurrencyCode);
    }

    public void SetPlannedAmount(Money plannedAmount)
    {
        ArgumentNullException.ThrowIfNull(plannedAmount, nameof(plannedAmount));
        if (plannedAmount.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(plannedAmount), "A planned amount cannot be negative.");
        PlannedAmount = plannedAmount;
    }

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void RecordActual(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        ActualAmount = ActualAmount.Add(amount);
    }

    public void Commit(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        CommittedAmount = CommittedAmount.Add(amount);
    }

    public void ReleaseCommitment(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (amount.Amount > CommittedAmount.Amount)
            throw new InvalidOperationException("Cannot release more than is committed.");
        CommittedAmount = CommittedAmount.Subtract(amount);
    }

    public bool IsOverspent => ActualAmount.Amount > PlannedAmount.Amount;

    /// <summary>Whether a further spend of this size would still fit inside the line.</summary>
    public bool CanAccommodate(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        return amount.Amount <= Available.Amount;
    }
}
