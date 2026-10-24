namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// A work trip. It is both an attendance fact, because the employee is away, and a cost,
/// because expenses are claimed against it.
/// </summary>
public sealed partial class Mission : AggregateRoot
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public string Title { get; private set; } = default!;
    public MissionType Type { get; private set; }
    public DateRange Period { get; private set; } = default!;

    public string Destination { get; private set; } = default!;
    public Guid? DestinationCityId { get; private set; }
    public string? Purpose { get; private set; }

    public HumanRequestStatus Status { get; private set; }
    public Guid? ApprovedByHumanId { get; private set; }
    public DateTimeOffset? ApprovedOn { get; private set; }

    /// <summary>The per day allowance agreed for this mission.</summary>
    public Money? DailyAllowance { get; private set; }

    public Guid? CostCenterId { get; private set; }
    public Guid? ProjectId { get; private set; }

    private readonly List<MissionExpense> _expenses = [];
    public IReadOnlyList<MissionExpense> Expenses => _expenses.AsReadOnly();

    private Mission() { }

    public Mission(Human human, string title, MissionType type, DateRange period, string destination)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(destination, nameof(destination));

        Human = human;
        HumanId = human.Id;
        Title = title.Trim();
        Type = type;
        Period = period;
        Destination = destination.Trim();
        Status = HumanRequestStatus.Pending;
    }

    public void SetPurpose(string? purpose) => Purpose = string.IsNullOrWhiteSpace(purpose) ? null : purpose.Trim();

    public void SetDestinationCity(Guid cityId) => DestinationCityId = cityId == Guid.Empty ? null : cityId;

    public void SetDailyAllowance(Money allowance)
    {
        ArgumentNullException.ThrowIfNull(allowance, nameof(allowance));
        if (allowance.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(allowance), "DailyAllowance cannot be negative.");
        DailyAllowance = allowance;
    }

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void AssignProject(Guid projectId) => ProjectId = projectId == Guid.Empty ? null : projectId;

    public void Approve(Guid approvedByHumanId)
    {
        if (Status != HumanRequestStatus.Pending)
            throw new InvalidOperationException($"The mission is already {Status}.");
        Status = HumanRequestStatus.Approved;
        ApprovedByHumanId = approvedByHumanId;
        ApprovedOn = DateTimeOffset.UtcNow;
    }

    public void Reject(Guid rejectedByHumanId)
    {
        if (Status != HumanRequestStatus.Pending)
            throw new InvalidOperationException($"The mission is already {Status}.");
        Status = HumanRequestStatus.Rejected;
        ApprovedByHumanId = rejectedByHumanId;
        ApprovedOn = DateTimeOffset.UtcNow;
    }

    public void AddExpense(MissionExpense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);
        EnsureCanAddChild(expense, expense.MissionId, _expenses);

        if (Status != HumanRequestStatus.Approved)
            throw new InvalidOperationException("Expenses can only be claimed against an approved mission.");
        _expenses.Add(expense);
    }

    /// <summary>Total claimed expenses, which requires every claim to share one currency.</summary>
    public Money? TotalExpenses()
    {
        if (_expenses.Count == 0)
            return null;
        return _expenses.Select(e => e.Amount).Aggregate((accumulated, next) => accumulated.Add(next));
    }

    public Money? TotalAllowance()
    {
        if (DailyAllowance is null || Period.TotalDays is null)
            return null;
        return DailyAllowance.Multiply(Period.TotalDays.Value);
    }
}
