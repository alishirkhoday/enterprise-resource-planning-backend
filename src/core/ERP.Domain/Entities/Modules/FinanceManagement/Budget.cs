namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// What the organization plans to spend or earn in a financial year, broken down by line.
/// Once approved it becomes the yardstick that actuals are compared against.
/// </summary>
public sealed class Budget : BaseEntity, IBranchScoped
{
    public Guid FinancialYearId { get; private set; }
    public string Title { get; private set; } = default!;
    public BudgetType Type { get; private set; }
    public BudgetStatus Status { get; private set; }

    public string CurrencyCode { get; private set; } = default!;
    public Guid? CostCenterId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? ProjectId { get; private set; }

    public int Version { get; private set; }
    public DateTimeOffset? ApprovedOn { get; private set; }
    public string? ApprovedBy { get; private set; }
    public string? Description { get; private set; }

    private readonly List<BudgetLine> _lines = [];
    public IReadOnlyList<BudgetLine> Lines => _lines;

    private Budget() { }

    public Budget(Guid financialYearId, string title, BudgetType type, string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        if (financialYearId == Guid.Empty)
            throw new ArgumentException("FinancialYearId is empty.", nameof(financialYearId));

        FinancialYearId = financialYearId;
        Title = title.Trim();
        Type = type;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        Status = BudgetStatus.Draft;
        Version = 1;
    }

    public void Update(string title, string? description)
    {
        EnsureDraft();
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void ScopeTo(Guid? costCenterId, Guid? branchId, Guid? projectId)
    {
        EnsureDraft();
        CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;
        BranchId = branchId == Guid.Empty ? null : branchId;
        ProjectId = projectId == Guid.Empty ? null : projectId;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void AddLine(BudgetLine line)
    {
        ArgumentNullException.ThrowIfNull(line, nameof(line));
        EnsureDraft();
        if (line.PlannedAmount.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Budget lines must be expressed in {CurrencyCode}.");
        if (_lines.Any(l => l.AccountId == line.AccountId && l.PeriodNumber == line.PeriodNumber))
            throw new InvalidOperationException("A line already exists for that account and period.");
        _lines.Add(line);
    }

    public void RemoveLine(BudgetLine line)
    {
        ArgumentNullException.ThrowIfNull(line, nameof(line));
        EnsureDraft();
        _lines.Remove(line);
    }

    public Money TotalPlanned() =>
        _lines.Count == 0
            ? Money.Zero(CurrencyCode)
            : _lines.Select(l => l.PlannedAmount).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money TotalActual() =>
        _lines.Count == 0
            ? Money.Zero(CurrencyCode)
            : _lines.Select(l => l.ActualAmount).Aggregate((accumulated, next) => accumulated.Add(next));

    public Money TotalVariance() => TotalPlanned().Subtract(TotalActual());

    public void Submit()
    {
        EnsureDraft();
        if (_lines.Count == 0)
            throw new InvalidOperationException("A budget cannot be submitted without lines.");
        Status = BudgetStatus.AwaitingApproval;
    }

    public void Approve(string? approvedBy = null)
    {
        if (Status != BudgetStatus.AwaitingApproval)
            throw new InvalidOperationException("Only a submitted budget can be approved.");
        Status = BudgetStatus.Approved;
        ApprovedOn = DateTimeOffset.UtcNow;
        ApprovedBy = approvedBy;
    }

    public void Reject()
    {
        if (Status != BudgetStatus.AwaitingApproval)
            throw new InvalidOperationException("Only a submitted budget can be rejected.");
        Status = BudgetStatus.Draft;
    }

    /// <summary>Opens a new revision, leaving the approved version untouched as history.</summary>
    public void Revise()
    {
        if (Status != BudgetStatus.Approved)
            throw new InvalidOperationException("Only an approved budget can be revised.");
        Version++;
        Status = BudgetStatus.Draft;
        ApprovedOn = null;
        ApprovedBy = null;
    }

    public void Close() => Status = BudgetStatus.Closed;

    private void EnsureDraft()
    {
        if (Status != BudgetStatus.Draft)
            throw new InvalidOperationException($"The budget is {Status} and can no longer be edited.");
    }
}
