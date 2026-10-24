namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// A periodic assessment of an employee. The score is expected to come from the goals in the
/// same period rather than being set arbitrarily.
/// </summary>
public sealed class HumanPerformanceReview : AggregateRoot
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public Human Reviewer { get; private set; } = default!;
    public Guid ReviewerId { get; private set; }

    public DateRange Period { get; private set; } = default!;
    public DateOnly ReviewDate { get; private set; }

    public int Score { get; private set; }
    public HumanPerformanceRating Rating { get; private set; }
    public PerformanceReviewStatus Status { get; private set; }

    public string? Strengths { get; private set; }
    public string? AreasForImprovement { get; private set; }
    public string? Comment { get; private set; }

    /// <summary>What the employee said in response, recorded before the review is finalised.</summary>
    public string? EmployeeComment { get; private set; }
    public DateOnly? AcknowledgedOn { get; private set; }

    private readonly List<PerformanceGoal> _goals = [];
    public IReadOnlyList<PerformanceGoal> Goals => _goals.AsReadOnly();

    private HumanPerformanceReview() { }

    public HumanPerformanceReview(Human human, Human reviewer, DateRange period, DateOnly reviewDate)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentNullException.ThrowIfNull(reviewer, nameof(reviewer));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        if (reviewer.Id == human.Id)
            throw new InvalidOperationException("An employee cannot review themselves.");

        Human = human;
        HumanId = human.Id;
        Reviewer = reviewer;
        ReviewerId = reviewer.Id;
        Period = period;
        ReviewDate = reviewDate;
        Status = PerformanceReviewStatus.Draft;
    }

    public void AddGoal(PerformanceGoal goal)
    {
        ArgumentNullException.ThrowIfNull(goal, nameof(goal));
        if (goal.HumanId != HumanId)
            throw new InvalidOperationException("The goal belongs to a different employee.");
        if (_goals.Any(g => g.Id == goal.Id))
            return;
        goal.LinkToReview(Id);
        _goals.Add(goal);
    }

    public void RecordAssessment(int score, string? strengths, string? areasForImprovement, string? comment)
    {
        EnsureDraft();
        if (score < 0 || score > 100)
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 0 and 100.");
        Score = score;
        Rating = RatingFor(score);
        Strengths = string.IsNullOrWhiteSpace(strengths) ? null : strengths.Trim();
        AreasForImprovement = string.IsNullOrWhiteSpace(areasForImprovement) ? null : areasForImprovement.Trim();
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }

    /// <summary>Derives the score from the weighted achievement of the linked goals.</summary>
    public void ScoreFromGoals()
    {
        EnsureDraft();
        if (_goals.Count == 0)
            throw new InvalidOperationException("There are no goals to score from.");

        var totalWeight = _goals.Sum(g => g.Weight.Value);
        if (totalWeight == 0m)
            throw new InvalidOperationException("The goals carry no weight.");

        var weighted = _goals.Sum(g => (g.AchievementPercentage() ?? 0m) * g.Weight.Value);
        var score = (int)Math.Round(Math.Min(weighted / totalWeight, 100m), MidpointRounding.AwayFromZero);
        Score = score;
        Rating = RatingFor(score);
    }

    public void Submit()
    {
        EnsureDraft();
        Status = PerformanceReviewStatus.AwaitingAcknowledgement;
    }

    public void Acknowledge(string? employeeComment, DateOnly acknowledgedOn)
    {
        if (Status != PerformanceReviewStatus.AwaitingAcknowledgement)
            throw new InvalidOperationException("The review is not awaiting acknowledgement.");
        EmployeeComment = string.IsNullOrWhiteSpace(employeeComment) ? null : employeeComment.Trim();
        AcknowledgedOn = acknowledgedOn;
        Status = PerformanceReviewStatus.Finalized;
    }

    private static HumanPerformanceRating RatingFor(int score) => score switch
    {
        >= 90 => HumanPerformanceRating.Excellent,
        >= 75 => HumanPerformanceRating.Good,
        >= 50 => HumanPerformanceRating.Average,
        >= 25 => HumanPerformanceRating.Poor,
        _ => HumanPerformanceRating.Unacceptable
    };

    private void EnsureDraft()
    {
        if (Status != PerformanceReviewStatus.Draft)
            throw new InvalidOperationException($"The review is {Status} and can no longer be edited.");
    }
}
