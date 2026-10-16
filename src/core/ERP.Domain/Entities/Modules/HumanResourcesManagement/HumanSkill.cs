namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// A skill somebody has, at a level. What a staffing decision is made from.
/// </summary>
public sealed class HumanSkill : BaseEntity
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public Skill Skill { get; private set; } = default!;
    public Guid SkillId { get; private set; }

    public SkillLevel Level { get; private set; }
    public decimal? YearsOfExperience { get; private set; }
    public DateOnly? AssessedOn { get; private set; }
    public string? AssessedBy { get; private set; }
    public string? Description { get; private set; }

    private HumanSkill() { }

    public HumanSkill(Human human, Skill skill, SkillLevel level)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentNullException.ThrowIfNull(skill, nameof(skill));
        Human = human;
        HumanId = human.Id;
        Skill = skill;
        SkillId = skill.Id;
        Level = level;
    }

    public void Assess(SkillLevel level, DateOnly assessedOn, string? assessedBy)
    {
        Level = level;
        AssessedOn = assessedOn;
        AssessedBy = string.IsNullOrWhiteSpace(assessedBy) ? null : assessedBy.Trim();
    }

    public void SetYearsOfExperience(decimal? years)
    {
        if (years.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegative(years.Value, nameof(years));
        YearsOfExperience = years;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
