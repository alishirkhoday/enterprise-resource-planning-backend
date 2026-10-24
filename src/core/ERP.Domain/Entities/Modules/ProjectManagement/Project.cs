using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Domain.Entities.Modules.ProjectManagement;

/// <summary>
/// A body of work with its own budget and schedule. Costs, hours and purchases can all be
/// attributed to it, which is what makes project profitability answerable.
/// </summary>
public sealed partial class Project : AggregateRoot, IBranchScoped
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public ProjectType Type { get; private set; }
    public ProjectStatus Status { get; private set; }

    public DateRange PlannedPeriod { get; private set; } = default!;
    public DateOnly? ActualStartDate { get; private set; }
    public DateOnly? ActualEndDate { get; private set; }

    public string CurrencyCode { get; private set; } = default!;
    public Money? ContractValue { get; private set; }
    public Money BudgetedCost { get; private set; } = default!;
    public Money ActualCost { get; private set; } = default!;
    public Money InvoicedAmount { get; private set; } = default!;

    /// <summary>Completion as reported by the team, which drives revenue recognition.</summary>
    public Percentage ProgressPercentage { get; private set; } = default!;

    public Customer? Customer { get; private set; }
    public Guid? CustomerId { get; private set; }

    public Guid? ManagerHumanId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? SalesContractId { get; private set; }

    public string? Description { get; private set; }

    private readonly List<ProjectPhase> _phases = [];
    public IReadOnlyList<ProjectPhase> Phases => _phases.AsReadOnly();

    private readonly List<ProjectMember> _members = [];
    public IReadOnlyList<ProjectMember> Members => _members.AsReadOnly();

    private Project() { }

    public Project(string code, string name, ProjectType type, DateRange plannedPeriod, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(plannedPeriod, nameof(plannedPeriod));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));

        Code = code.Trim();
        Name = name.Trim();
        Type = type;
        PlannedPeriod = plannedPeriod;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        BudgetedCost = Money.Zero(CurrencyCode);
        ActualCost = Money.Zero(CurrencyCode);
        InvoicedAmount = Money.Zero(CurrencyCode);
        ProgressPercentage = Percentage.Zero();
        Status = ProjectStatus.Draft;
    }

    public void Update(string name, DateRange plannedPeriod, string? description)
    {
        ArgumentNullException.ThrowIfNull(plannedPeriod, nameof(plannedPeriod));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        PlannedPeriod = plannedPeriod;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void AttachToCustomer(Customer customer, Guid? salesContractId = null)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        Customer = customer;
        CustomerId = customer.Id;
        SalesContractId = salesContractId == Guid.Empty ? null : salesContractId;
    }

    public void SetContractValue(Money contractValue)
    {
        ArgumentNullException.ThrowIfNull(contractValue, nameof(contractValue));
        if (contractValue.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"The contract value must be expressed in {CurrencyCode}.", nameof(contractValue));
        ContractValue = contractValue;
    }

    public void SetBudget(Money budgetedCost)
    {
        ArgumentNullException.ThrowIfNull(budgetedCost, nameof(budgetedCost));
        if (budgetedCost.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"The budget must be expressed in {CurrencyCode}.", nameof(budgetedCost));
        if (budgetedCost.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(budgetedCost), "A budget cannot be negative.");
        BudgetedCost = budgetedCost;
    }

    public void AssignManager(Guid humanId) => ManagerHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void AddPhase(ProjectPhase phase)
    {
        ArgumentNullException.ThrowIfNull(phase);
        EnsureCanAddChild(phase, phase.ProjectId, _phases);

        if (_phases.Any(p => p.SequenceNumber == phase.SequenceNumber))
            throw new InvalidOperationException($"Phase {phase.SequenceNumber} already exists on this project.");
        _phases.Add(phase);
    }

    public void AddMember(ProjectMember member)
    {
        ArgumentNullException.ThrowIfNull(member);
        EnsureCanAddChild(member, member.ProjectId, _members);

        if (_members.Any(m => m.HumanId == member.HumanId && m.IsCurrentlyActive))
            throw new InvalidOperationException("That person is already on the project.");
        _members.Add(member);
    }

    public void Start(DateOnly actualStartDate)
    {
        if (Status != ProjectStatus.Draft)
            throw new InvalidOperationException($"Only a draft project can be started; this one is {Status}.");
        ActualStartDate = actualStartDate;
        Status = ProjectStatus.Active;
    }

    public void RecordCost(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        ActualCost = ActualCost.Add(amount);
    }

    public void RecordInvoiced(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        InvoicedAmount = InvoicedAmount.Add(amount);
    }

    public void ReportProgress(Percentage progressPercentage)
    {
        ArgumentNullException.ThrowIfNull(progressPercentage, nameof(progressPercentage));
        if (Status != ProjectStatus.Active)
            throw new InvalidOperationException("Progress can only be reported on an active project.");
        ProgressPercentage = progressPercentage;
    }

    /// <summary>Recomputes progress from the phases, weighted by their share of the work.</summary>
    public void RecalculateProgressFromPhases()
    {
        if (_phases.Count == 0)
            return;
        var totalWeight = _phases.Sum(p => p.Weight.Value);
        if (totalWeight == 0m)
            return;
        var weighted = _phases.Sum(p => p.ProgressPercentage.Value * p.Weight.Value) / totalWeight;
        ProgressPercentage = Percentage.Create(Math.Min(Math.Round(weighted, 2), 100m));
    }

    public void Hold(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != ProjectStatus.Active)
            throw new InvalidOperationException("Only an active project can be put on hold.");
        Status = ProjectStatus.OnHold;
        Description = reason.Trim();
    }

    public void Resume()
    {
        if (Status != ProjectStatus.OnHold)
            throw new InvalidOperationException("Only a project on hold can be resumed.");
        Status = ProjectStatus.Active;
    }

    public void Complete(DateOnly actualEndDate)
    {
        if (Status is not (ProjectStatus.Active or ProjectStatus.OnHold))
            throw new InvalidOperationException($"A project in state {Status} cannot be completed.");
        ActualEndDate = actualEndDate;
        ProgressPercentage = Percentage.Create(100m);
        Status = ProjectStatus.Completed;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == ProjectStatus.Completed)
            throw new InvalidOperationException("A completed project cannot be cancelled.");
        Status = ProjectStatus.Cancelled;
        Description = reason.Trim();
    }

    public Money CostVariance => BudgetedCost.Subtract(ActualCost);

    public bool IsOverBudget => ActualCost.Amount > BudgetedCost.Amount;

    public Money? Margin => ContractValue?.Subtract(ActualCost);

    public bool IsBehindSchedule(DateOnly asOf) =>
        Status == ProjectStatus.Active
        && PlannedPeriod.EndDate.HasValue
        && asOf > PlannedPeriod.EndDate.Value
        && ProgressPercentage.Value < 100m;
}
