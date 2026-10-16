namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// A qualification somebody holds - سوابق تحصیلی.
/// </summary>
public sealed class HumanEducation : BaseEntity
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }
    public HumanEducationFieldOfStudy FieldOfStudy { get; private set; } = default!;
    public Guid FieldOfStudyId { get; private set; }
    public HumanEducationUniversity University { get; private set; } = default!;
    public Guid UniversityId { get; private set; }
    public HumanEducationDegree Degree { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? GraduationDate { get; private set; }

    private HumanEducation() { }

    public HumanEducation(Human human, HumanEducationFieldOfStudy fieldOfStudy, HumanEducationUniversity university, HumanEducationDegree degree, DateOnly startDate, DateOnly? graduationDate = null)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentNullException.ThrowIfNull(fieldOfStudy, nameof(fieldOfStudy));
        ArgumentNullException.ThrowIfNull(university, nameof(university));
        Human = human;
        HumanId = human.Id;
        FieldOfStudy = fieldOfStudy;
        FieldOfStudyId = fieldOfStudy.Id;
        University = university;
        UniversityId = university.Id;
        Degree = degree;
        StartDate = startDate;
        GraduationDate = graduationDate;
    }

    public void Update(HumanEducationFieldOfStudy fieldOfStudy, HumanEducationUniversity university, HumanEducationDegree degree, DateOnly startDate, DateOnly? graduationDate = null)
    {
        ArgumentNullException.ThrowIfNull(fieldOfStudy, nameof(fieldOfStudy));
        ArgumentNullException.ThrowIfNull(university, nameof(university));
        FieldOfStudy = fieldOfStudy;
        FieldOfStudyId = fieldOfStudy.Id;
        University = university;
        UniversityId = university.Id;
        Degree = degree;
        StartDate = startDate;
        GraduationDate = graduationDate;
    }

    public void SetGraduationDate(DateOnly graduationDate) => GraduationDate = graduationDate;
}
