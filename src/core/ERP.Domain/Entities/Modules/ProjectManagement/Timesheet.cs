using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Domain.Entities.Modules.ProjectManagement;

/// <summary>
/// One employee record of how a period was spent. It is the single place hours are captured for
/// payroll, for project costing and for billing the customer.
/// </summary>
public sealed partial class Timesheet : AggregateRoot
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public DateRange Period { get; private set; } = default!;
    public TimesheetStatus Status { get; private set; }

    public DateTimeOffset? SubmittedOn { get; private set; }
    public DateTimeOffset? ApprovedOn { get; private set; }
    public Guid? ApprovedByHumanId { get; private set; }
    public string? RejectionReason { get; private set; }

    private readonly List<TimesheetEntry> _entries = [];
    public IReadOnlyList<TimesheetEntry> Entries => _entries.AsReadOnly();

    private Timesheet() { }

    public Timesheet(Human human, DateRange period)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        if (period.EndDate is null)
            throw new ArgumentException("A timesheet covers a finite period.", nameof(period));

        Human = human;
        HumanId = human.Id;
        Period = period;
        Status = TimesheetStatus.Draft;
    }

    public void AddEntry(TimesheetEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        EnsureCanAddChild(entry, entry.TimesheetId, _entries);

        EnsureEditable();
        if (!Period.Contains(entry.WorkDate))
            throw new InvalidOperationException($"{entry.WorkDate:yyyy/MM/dd} falls outside the timesheet period.");

        var dayTotal = _entries.Where(e => e.WorkDate == entry.WorkDate).Sum(e => e.Hours) + entry.Hours;
        if (dayTotal > 24m)
            throw new InvalidOperationException($"More than 24 hours cannot be booked on {entry.WorkDate:yyyy/MM/dd}.");

        _entries.Add(entry);
    }

    public void RemoveEntry(TimesheetEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.TimesheetId != Id || !_entries.Contains(entry))
            throw new InvalidOperationException("The child is not attached to this owner.");

        ArgumentNullException.ThrowIfNull(entry, nameof(entry));
        EnsureEditable();
        _entries.Remove(entry);
    }

    public decimal TotalHours => _entries.Sum(e => e.Hours);
    public decimal BillableHours => _entries.Where(e => e.IsBillable).Sum(e => e.Hours);
    public decimal NonBillableHours => TotalHours - BillableHours;

    public decimal UtilizationPercentage => TotalHours == 0m ? 0m : BillableHours / TotalHours * 100m;

    public decimal HoursOn(DateOnly date) => _entries.Where(e => e.WorkDate == date).Sum(e => e.Hours);

    public void Submit()
    {
        EnsureEditable();
        if (_entries.Count == 0)
            throw new InvalidOperationException("An empty timesheet cannot be submitted.");
        Status = TimesheetStatus.Submitted;
        SubmittedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Approves the sheet and books its hours onto the tasks. Hours only count once the sheet
    /// has been approved, so a draft never inflates a project.
    /// </summary>
    public void Approve(Guid approvedByHumanId)
    {
        if (Status != TimesheetStatus.Submitted)
            throw new InvalidOperationException($"Only a submitted timesheet can be approved; this one is {Status}.");
        if (approvedByHumanId == HumanId)
            throw new InvalidOperationException("A timesheet cannot be approved by the person who submitted it.");

        foreach (var entry in _entries.Where(e => e.ProjectTask is not null))
        {
            entry.ProjectTask!.RecordHours(entry.Hours);
        }

        ApprovedByHumanId = approvedByHumanId;
        ApprovedOn = DateTimeOffset.UtcNow;
        Status = TimesheetStatus.Approved;
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != TimesheetStatus.Submitted)
            throw new InvalidOperationException($"Only a submitted timesheet can be rejected; this one is {Status}.");
        Status = TimesheetStatus.Rejected;
        RejectionReason = reason.Trim();
    }

    /// <summary>Sends a rejected sheet back to draft so it can be corrected.</summary>
    public void Reopen()
    {
        if (Status != TimesheetStatus.Rejected)
            throw new InvalidOperationException("Only a rejected timesheet can be reopened.");
        Status = TimesheetStatus.Draft;
        RejectionReason = null;
        SubmittedOn = null;
    }

    private void EnsureEditable()
    {
        if (Status is not (TimesheetStatus.Draft or TimesheetStatus.Rejected))
            throw new InvalidOperationException($"The timesheet is {Status} and can no longer be edited.");
    }
}
