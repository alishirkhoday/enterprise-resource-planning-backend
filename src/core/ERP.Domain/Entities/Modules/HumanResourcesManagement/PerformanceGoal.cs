namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Something an employee is expected to achieve in a period, measured against a target.
/// Reviews score the goals rather than inventing a number from nothing.
/// </summary>
public sealed class PerformanceGoal : AggregateRoot
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateRange Period { get; private set; } = default!;

    /// <summary>How the goal is measured, for example Number of closed tickets.</summary>
    public string? MetricName { get; private set; }
    public decimal? TargetValue { get; private set; }
    public decimal? ActualValue { get; private set; }

    /// <summary>Relative importance of this goal within the review, as a percentage.</summary>
    public Percentage Weight { get; private set; } = default!;

    public PerformanceGoalStatus Status { get; private set; }
    public Guid? ReviewId { get; private set; }

    private PerformanceGoal() { }

    public PerformanceGoal(Human human, string title, DateRange period, Percentage weight)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        ArgumentNullException.ThrowIfNull(weight, nameof(weight));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        Human = human;
        HumanId = human.Id;
        Title = title.Trim();
        Period = period;
        Weight = weight;
        Status = PerformanceGoalStatus.Draft;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void SetMetric(string metricName, decimal targetValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metricName, nameof(metricName));
        MetricName = metricName.Trim();
        TargetValue = targetValue;
    }

    public void Activate()
    {
        if (Status != PerformanceGoalStatus.Draft)
            throw new InvalidOperationException($"Only a draft goal can be activated; this one is {Status}.");
        Status = PerformanceGoalStatus.Active;
    }

    public void RecordProgress(decimal actualValue)
    {
        if (Status != PerformanceGoalStatus.Active)
            throw new InvalidOperationException("Progress can only be recorded against an active goal.");
        ActualValue = actualValue;
    }

    public void Complete()
    {
        if (Status != PerformanceGoalStatus.Active)
            throw new InvalidOperationException("Only an active goal can be completed.");
        Status = PerformanceGoalStatus.Completed;
    }

    public void Cancel(string? reason = null)
    {
        Status = PerformanceGoalStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
            Description = reason.Trim();
    }

    public void LinkToReview(Guid reviewId) => ReviewId = reviewId == Guid.Empty ? null : reviewId;

    /// <summary>Achievement as a percentage of target, capped at two hundred percent.</summary>
    public decimal? AchievementPercentage()
    {
        if (TargetValue is null || TargetValue.Value == 0m || ActualValue is null)
            return null;
        return Math.Min(ActualValue.Value / TargetValue.Value * 100m, 200m);
    }
}
