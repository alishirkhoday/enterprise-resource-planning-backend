namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// An open vacancy against a position. Closing it is what ends the hiring process.
/// </summary>
public sealed class JobPosting : AggregateRoot, IBranchScoped
{
    public Position Position { get; private set; } = default!;
    public Guid PositionId { get; private set; }

    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? Requirements { get; private set; }

    public int Openings { get; private set; }
    public HumanEmploymentType EmploymentType { get; private set; }
    public JobPostingStatus Status { get; private set; }

    public DateOnly PublishDate { get; private set; }
    public DateOnly? ClosingDate { get; private set; }

    public Money? SalaryRangeFrom { get; private set; }
    public Money? SalaryRangeTo { get; private set; }

    public Guid? BranchId { get; private set; }
    public Guid? HiringManagerHumanId { get; private set; }

    private readonly List<JobApplication> _applications = [];
    public IReadOnlyList<JobApplication> Applications => _applications.AsReadOnly();

    private JobPosting() { }

    public JobPosting(Position position, string title, HumanEmploymentType employmentType, DateOnly publishDate, int openings = 1)
    {
        ArgumentNullException.ThrowIfNull(position, nameof(position));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(openings, nameof(openings));

        Position = position;
        PositionId = position.Id;
        Title = title.Trim();
        EmploymentType = employmentType;
        PublishDate = publishDate;
        Openings = openings;
        Status = JobPostingStatus.Draft;
    }

    public void Describe(string? description, string? requirements)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Requirements = string.IsNullOrWhiteSpace(requirements) ? null : requirements.Trim();
    }

    public void SetSalaryRange(Money from, Money to)
    {
        ArgumentNullException.ThrowIfNull(from, nameof(from));
        ArgumentNullException.ThrowIfNull(to, nameof(to));
        if (from.CurrencyCode != to.CurrencyCode)
            throw new ArgumentException("Both ends of the salary range must use one currency.", nameof(to));
        if (to.Amount < from.Amount)
            throw new ArgumentOutOfRangeException(nameof(to), "The upper bound cannot be lower than the lower bound.");
        SalaryRangeFrom = from;
        SalaryRangeTo = to;
    }

    public void AssignHiringManager(Guid humanId) => HiringManagerHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void Publish(DateOnly? closingDate = null)
    {
        if (Status != JobPostingStatus.Draft)
            throw new InvalidOperationException($"Only a draft posting can be published; this one is {Status}.");
        if (closingDate.HasValue && closingDate.Value < PublishDate)
            throw new ArgumentOutOfRangeException(nameof(closingDate), "ClosingDate cannot be earlier than PublishDate.");
        ClosingDate = closingDate;
        Status = JobPostingStatus.Published;
    }

    public void AddApplication(JobApplication application)
    {
        ArgumentNullException.ThrowIfNull(application, nameof(application));
        if (Status != JobPostingStatus.Published)
            throw new InvalidOperationException("Applications are only accepted while the posting is published.");
        if (_applications.Any(a => a.CandidateId == application.CandidateId))
            throw new InvalidOperationException("This candidate has already applied to this posting.");
        _applications.Add(application);
    }

    public void Close()
    {
        if (Status is JobPostingStatus.Closed or JobPostingStatus.Cancelled)
            return;
        Status = JobPostingStatus.Closed;
    }

    public void Cancel()
    {
        if (Status == JobPostingStatus.Closed)
            throw new InvalidOperationException("A closed posting cannot be cancelled.");
        Status = JobPostingStatus.Cancelled;
    }

    public int HiredCount => _applications.Count(a => a.Status == JobApplicationStatus.Hired);

    public bool HasRemainingOpenings => HiredCount < Openings;
}
