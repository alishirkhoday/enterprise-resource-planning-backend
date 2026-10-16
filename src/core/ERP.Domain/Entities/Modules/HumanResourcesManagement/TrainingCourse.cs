namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// A course the organization runs or buys in. Sessions are the concrete runs of it.
/// </summary>
public sealed class TrainingCourse : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? Category { get; private set; }

    public decimal DurationHours { get; private set; }
    public bool IsMandatory { get; private set; }

    /// <summary>How long the resulting certificate stays valid, when it expires at all.</summary>
    public int? ValidityMonths { get; private set; }

    public Money? CostPerParticipant { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<TrainingSession> _sessions = [];
    public IReadOnlyList<TrainingSession> Sessions => _sessions;

    private readonly List<Skill> _skillsCovered = [];
    public IReadOnlyList<Skill> SkillsCovered => _skillsCovered;

    private TrainingCourse() { }

    public TrainingCourse(string code, string title, decimal durationHours)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationHours, nameof(durationHours));
        Code = code.Trim();
        Title = title.Trim();
        DurationHours = durationHours;
        IsActive = true;
    }

    public void Update(string title, decimal durationHours, string? category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationHours, nameof(durationHours));
        Title = title.Trim();
        DurationHours = durationHours;
        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void MarkAsMandatory() => IsMandatory = true;
    public void MarkAsOptional() => IsMandatory = false;

    public void SetValidity(int? validityMonths)
    {
        if (validityMonths.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(validityMonths.Value, nameof(validityMonths));
        ValidityMonths = validityMonths;
    }

    public void SetCost(Money costPerParticipant)
    {
        ArgumentNullException.ThrowIfNull(costPerParticipant, nameof(costPerParticipant));
        if (costPerParticipant.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(costPerParticipant), "Cost cannot be negative.");
        CostPerParticipant = costPerParticipant;
    }

    public void CoverSkill(Skill skill)
    {
        ArgumentNullException.ThrowIfNull(skill, nameof(skill));
        if (_skillsCovered.Any(s => s.Id == skill.Id))
            return;
        _skillsCovered.Add(skill);
    }

    public void AddSession(TrainingSession session)
    {
        ArgumentNullException.ThrowIfNull(session, nameof(session));
        _sessions.Add(session);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
