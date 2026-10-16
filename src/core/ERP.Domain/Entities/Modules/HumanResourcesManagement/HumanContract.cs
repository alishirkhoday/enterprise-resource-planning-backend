namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// The employment agreement that places a person in a position for a period, at an agreed wage.
/// Payroll reads the contract that is in force for the period being paid.
/// </summary>
public sealed class HumanContract : BaseEntity
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public Position Position { get; private set; } = default!;
    public Guid PositionId { get; private set; }

    public string? ContractNumber { get; private set; }
    public HumanEmploymentType EmploymentType { get; private set; }

    public DateRange Period { get; private set; } = default!;
    public DateOnly StartDate => Period.StartDate;
    public DateOnly? EndDate => Period.EndDate;

    public DateOnly? ProbationEndDate { get; private set; }
    public DateOnly? TerminationDate { get; private set; }
    public string? TerminationDescription { get; private set; }

    /// <summary>The agreed base wage for a full period, before any addition or deduction.</summary>
    public Money? BaseSalary { get; private set; }

    /// <summary>Contracted working hours per week, used to prorate part time wages.</summary>
    public decimal? WeeklyWorkingHours { get; private set; }

    public Guid? CostCenterId { get; private set; }
    public Guid? WorkCalendarId { get; private set; }
    public string? Description { get; private set; }

    private HumanContract() { }

    public HumanContract(Human human, Position position, HumanEmploymentType employmentType, DateRange period)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentNullException.ThrowIfNull(position, nameof(position));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        Human = human;
        HumanId = human.Id;
        Position = position;
        PositionId = position.Id;
        EmploymentType = employmentType;
        Period = period;
    }

    public void Update(Position position, HumanEmploymentType employmentType, DateRange period)
    {
        ArgumentNullException.ThrowIfNull(position, nameof(position));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        if (TerminationDate is not null)
            throw new InvalidOperationException("A terminated contract cannot be updated.");
        Position = position;
        PositionId = position.Id;
        EmploymentType = employmentType;
        Period = period;
    }

    public void SetContractNumber(string? contractNumber) =>
        ContractNumber = string.IsNullOrWhiteSpace(contractNumber) ? null : contractNumber.Trim();

    public void SetBaseSalary(Money baseSalary)
    {
        ArgumentNullException.ThrowIfNull(baseSalary, nameof(baseSalary));
        if (baseSalary.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(baseSalary), "BaseSalary cannot be negative.");
        BaseSalary = baseSalary;
    }

    public void SetWeeklyWorkingHours(decimal? hours)
    {
        if (hours.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hours.Value, nameof(hours));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(hours.Value, 168m, nameof(hours));
        }
        WeeklyWorkingHours = hours;
    }

    public void SetProbationEnd(DateOnly probationEndDate)
    {
        if (probationEndDate < Period.StartDate)
            throw new ArgumentOutOfRangeException(nameof(probationEndDate), "ProbationEndDate cannot be earlier than the contract start.");
        ProbationEndDate = probationEndDate;
    }

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void AssignWorkCalendar(Guid workCalendarId) => WorkCalendarId = workCalendarId == Guid.Empty ? null : workCalendarId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void Terminate(DateOnly terminationDate, string? reason = null)
    {
        if (terminationDate < Period.StartDate)
            throw new ArgumentOutOfRangeException(nameof(terminationDate), "TerminationDate cannot be earlier than the contract start.");
        TerminationDate = terminationDate;
        TerminationDescription = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public bool IsInProbationOn(DateOnly date) => ProbationEndDate.HasValue && date <= ProbationEndDate.Value && Period.Contains(date);

    public bool IsEffectiveOn(DateOnly date) =>
        Period.Contains(date) && (TerminationDate is null || date <= TerminationDate.Value);
}
