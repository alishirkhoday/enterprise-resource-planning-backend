using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Domain.Entities.Modules.ProjectManagement;

/// <summary>
/// Someone working on a project, with the share of their time it takes and the rate their hours
/// are charged at.
/// </summary>
public sealed class ProjectMember : BaseEntity
{
    public Project Project { get; private set; } = default!;
    public Guid ProjectId { get; private set; }

    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public string Role { get; private set; } = default!;
    public DateRange Period { get; private set; } = default!;

    /// <summary>The share of their working time committed to this project.</summary>
    public Percentage Allocation { get; private set; } = default!;

    /// <summary>What an hour of their time costs the organization.</summary>
    public Money? HourlyCostRate { get; private set; }

    /// <summary>What an hour of their time is charged to the customer at.</summary>
    public Money? HourlyBillingRate { get; private set; }

    public DateOnly? LeftOn { get; private set; }

    private ProjectMember() { }

    public ProjectMember(Project project, Human human, string role, DateRange period, Percentage allocation)
    {
        ArgumentNullException.ThrowIfNull(project, nameof(project));
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        ArgumentNullException.ThrowIfNull(allocation, nameof(allocation));
        ArgumentException.ThrowIfNullOrWhiteSpace(role, nameof(role));
        if (human.Status != HumanStatus.Active)
            throw new InvalidOperationException($"Employee {human.FullName} is {human.Status} and cannot join a project.");

        Project = project;
        ProjectId = project.Id;
        Human = human;
        HumanId = human.Id;
        Role = role.Trim();
        Period = period;
        Allocation = allocation;
    }

    public void SetRates(Money? hourlyCostRate, Money? hourlyBillingRate)
    {
        HourlyCostRate = hourlyCostRate;
        HourlyBillingRate = hourlyBillingRate;
    }

    public void ChangeAllocation(Percentage allocation)
    {
        ArgumentNullException.ThrowIfNull(allocation, nameof(allocation));
        Allocation = allocation;
    }

    public void Leave(DateOnly leftOn) => LeftOn = leftOn;

    public bool IsCurrentlyActive =>
        LeftOn is null && Period.Contains(DateOnly.FromDateTime(DateTime.UtcNow));

    public Money? CostOf(decimal hours)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(hours, nameof(hours));
        return HourlyCostRate?.Multiply(hours);
    }

    public Money? BillableValueOf(decimal hours)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(hours, nameof(hours));
        return HourlyBillingRate?.Multiply(hours);
    }
}
