namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// A grouping of machines and people that performs one kind of operation, with a capacity and a
/// cost per hour. Routings schedule against work centres, not against individual machines.
/// </summary>
public sealed class WorkCenter : BaseEntity, IBranchScoped
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    /// <summary>How many jobs the centre can run at once.</summary>
    public int Capacity { get; private set; }

    /// <summary>Hours the centre is available on a normal working day.</summary>
    public decimal DailyCapacityHours { get; private set; }

    /// <summary>Proportion of nominal capacity actually achieved, allowing for setup and losses.</summary>
    public Percentage Efficiency { get; private set; } = default!;

    /// <summary>What an hour at this centre costs, used to value production.</summary>
    public Money? HourlyRate { get; private set; }

    public Guid? CostCenterId { get; private set; }
    public Guid? WorkStationId { get; private set; }
    public Guid? BranchId { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<Machine> _machines = [];
    public IReadOnlyList<Machine> Machines => _machines;

    private WorkCenter() { }

    public WorkCenter(string code, string name, int capacity = 1, decimal dailyCapacityHours = 8m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity, nameof(capacity));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dailyCapacityHours, nameof(dailyCapacityHours));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dailyCapacityHours, 24m, nameof(dailyCapacityHours));

        Code = code.Trim();
        Name = name.Trim();
        Capacity = capacity;
        DailyCapacityHours = dailyCapacityHours;
        Efficiency = Percentage.Create(100m);
        IsActive = true;
    }

    public void Update(string name, int capacity, decimal dailyCapacityHours)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity, nameof(capacity));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dailyCapacityHours, nameof(dailyCapacityHours));
        Name = name.Trim();
        Capacity = capacity;
        DailyCapacityHours = dailyCapacityHours;
    }

    public void SetEfficiency(Percentage efficiency)
    {
        ArgumentNullException.ThrowIfNull(efficiency, nameof(efficiency));
        Efficiency = efficiency;
    }

    public void SetHourlyRate(Money hourlyRate)
    {
        ArgumentNullException.ThrowIfNull(hourlyRate, nameof(hourlyRate));
        if (hourlyRate.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(hourlyRate), "An hourly rate cannot be negative.");
        HourlyRate = hourlyRate;
    }

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void AssignWorkStation(Guid workStationId) => WorkStationId = workStationId == Guid.Empty ? null : workStationId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddMachine(Machine machine)
    {
        ArgumentNullException.ThrowIfNull(machine, nameof(machine));
        if (_machines.Any(m => m.Id == machine.Id))
            return;
        _machines.Add(machine);
    }

    /// <summary>Hours actually available per day once capacity and efficiency are taken into account.</summary>
    public decimal EffectiveDailyHours => DailyCapacityHours * Capacity * Efficiency.AsFraction();

    /// <summary>The cost of occupying the centre for a number of hours.</summary>
    public Money? CostOf(decimal hours)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(hours, nameof(hours));
        return HourlyRate?.Multiply(hours);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
