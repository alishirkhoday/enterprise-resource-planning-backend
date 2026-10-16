namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Work somebody did before joining - سوابق کاری. Kept because Iranian severance and leave
/// entitlements can depend on prior service.
/// </summary>
public sealed class HumanWorkExperience : BaseEntity
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }
    public Company Company { get; private set; } = default!;
    public Guid CompanyId { get; private set; }
    public Job Job { get; private set; } = default!;
    public Guid JobId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string? Description { get; private set; }

    private HumanWorkExperience() { }

    public HumanWorkExperience(Human human, Company company, Job job, DateOnly startDate, DateOnly? endDate = null)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentNullException.ThrowIfNull(company, nameof(company));
        ArgumentNullException.ThrowIfNull(job, nameof(job));
        Human = human;
        HumanId = human.Id;
        Company = company;
        CompanyId = company.Id;
        Job = job;
        JobId = job.Id;
        StartDate = startDate;
        EndDate = endDate;
    }

    public void Update(Company company, Job job, DateOnly startDate, DateOnly? endDate = null)
    {
        ArgumentNullException.ThrowIfNull(company, nameof(company));
        ArgumentNullException.ThrowIfNull(job, nameof(job));
        Company = company;
        CompanyId = company.Id;
        Job = job;
        JobId = job.Id;
        StartDate = startDate;
        EndDate = endDate;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();
}
