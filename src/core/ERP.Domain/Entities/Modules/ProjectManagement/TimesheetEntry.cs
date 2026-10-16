namespace ERP.Domain.Entities.Modules.ProjectManagement;

/// <summary>
/// Hours booked on one day against one task, or against an activity that belongs to no project.
/// </summary>
public sealed class TimesheetEntry : BaseEntity
{
    public Timesheet Timesheet { get; private set; } = default!;
    public Guid TimesheetId { get; private set; }

    public DateOnly WorkDate { get; private set; }
    public decimal Hours { get; private set; }

    public ProjectTask? ProjectTask { get; private set; }
    public Guid? ProjectTaskId { get; private set; }
    public Guid? ProjectId { get; private set; }

    /// <summary>Set when the time belongs to no project, such as internal training.</summary>
    public TimesheetActivityType? ActivityType { get; private set; }

    public Guid? CostCenterId { get; private set; }
    public bool IsBillable { get; private set; }

    /// <summary>The rate the hours are charged out at, captured when the entry is made.</summary>
    public Money? BillingRate { get; private set; }

    public string? Description { get; private set; }

    public Money? BillableValue => IsBillable ? BillingRate?.Multiply(Hours) : null;

    private TimesheetEntry() { }

    public TimesheetEntry(Timesheet timesheet, DateOnly workDate, decimal hours, ProjectTask projectTask)
    {
        ArgumentNullException.ThrowIfNull(timesheet, nameof(timesheet));
        ArgumentNullException.ThrowIfNull(projectTask, nameof(projectTask));
        EnsureValidHours(hours);

        Timesheet = timesheet;
        TimesheetId = timesheet.Id;
        WorkDate = workDate;
        Hours = hours;
        ProjectTask = projectTask;
        ProjectTaskId = projectTask.Id;
        ProjectId = projectTask.ProjectId;
        IsBillable = projectTask.IsBillable;
    }

    public TimesheetEntry(Timesheet timesheet, DateOnly workDate, decimal hours, TimesheetActivityType activityType)
    {
        ArgumentNullException.ThrowIfNull(timesheet, nameof(timesheet));
        EnsureValidHours(hours);

        Timesheet = timesheet;
        TimesheetId = timesheet.Id;
        WorkDate = workDate;
        Hours = hours;
        ActivityType = activityType;
        IsBillable = false;
    }

    public void SetBillingRate(Money billingRate)
    {
        ArgumentNullException.ThrowIfNull(billingRate, nameof(billingRate));
        if (billingRate.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(billingRate), "A billing rate cannot be negative.");
        BillingRate = billingRate;
    }

    public void MarkAsNonBillable() => IsBillable = false;

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static void EnsureValidHours(decimal hours)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hours, nameof(hours));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hours, 24m, nameof(hours));
    }
}
