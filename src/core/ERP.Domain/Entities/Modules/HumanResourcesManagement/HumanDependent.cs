namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// A family member the employee supports. Insurance enrolment and payroll tax relief both
/// depend on knowing who they are.
/// </summary>
public sealed class HumanDependent : BaseEntity
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public string Name { get; private set; } = default!;
    public string Family { get; private set; } = default!;
    public DependentRelationship Relationship { get; private set; }
    public HumanGender Gender { get; private set; }
    public DateOnly BirthDate { get; private set; }
    public NationalId? NationalId { get; private set; }

    /// <summary>Whether the dependent is covered by the employer provided insurance.</summary>
    public bool IsInsured { get; private set; }

    /// <summary>Whether the dependent counts towards payroll tax relief.</summary>
    public bool IsTaxDependent { get; private set; }

    public bool HasSpecialNeeds { get; private set; }
    public string? Description { get; private set; }

    private HumanDependent() { }

    public HumanDependent(Human human, string name, string family, DependentRelationship relationship, HumanGender gender, DateOnly birthDate)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(family, nameof(family));
        if (birthDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentOutOfRangeException(nameof(birthDate), "BirthDate cannot be in the future.");

        Human = human;
        HumanId = human.Id;
        Name = name.Trim();
        Family = family.Trim();
        Relationship = relationship;
        Gender = gender;
        BirthDate = birthDate;
    }

    internal void SetNationalId(string nationalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nationalId, nameof(nationalId));
        NationalId = NationalId.Create(nationalId);
    }

    internal void SetCoverage(bool isInsured, bool isTaxDependent)
    {
        IsInsured = isInsured;
        IsTaxDependent = isTaxDependent;
    }

    internal void MarkAsHavingSpecialNeeds() => HasSpecialNeeds = true;

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public int AgeOn(DateOnly date)
    {
        var age = date.Year - BirthDate.Year;
        if (BirthDate > date.AddYears(-age))
            age--;
        return age;
    }
}
