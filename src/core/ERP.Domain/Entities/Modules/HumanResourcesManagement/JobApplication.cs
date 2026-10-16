namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// One candidate pursuing one vacancy, together with the interviews held along the way.
/// </summary>
public sealed class JobApplication : BaseEntity
{
    public JobPosting Posting { get; private set; } = default!;
    public Guid PostingId { get; private set; }

    public Candidate Candidate { get; private set; } = default!;
    public Guid CandidateId { get; private set; }

    public DateOnly AppliedOn { get; private set; }
    public JobApplicationStatus Status { get; private set; }
    public string? CoverLetter { get; private set; }
    public string? RejectionReason { get; private set; }

    public Money? ExpectedSalary { get; private set; }

    /// <summary>Screening score out of one hundred, set when the resume is reviewed.</summary>
    public int? ScreeningScore { get; private set; }

    private readonly List<Interview> _interviews = [];
    public IReadOnlyList<Interview> Interviews => _interviews;

    private JobApplication() { }

    public JobApplication(JobPosting posting, Candidate candidate, DateOnly appliedOn)
    {
        ArgumentNullException.ThrowIfNull(posting, nameof(posting));
        ArgumentNullException.ThrowIfNull(candidate, nameof(candidate));
        if (candidate.IsBlacklisted)
            throw new InvalidOperationException("A blacklisted candidate cannot apply.");

        Posting = posting;
        PostingId = posting.Id;
        Candidate = candidate;
        CandidateId = candidate.Id;
        AppliedOn = appliedOn;
        Status = JobApplicationStatus.Received;
    }

    public void SetCoverLetter(string? coverLetter) => CoverLetter = string.IsNullOrWhiteSpace(coverLetter) ? null : coverLetter.Trim();

    public void SetExpectedSalary(Money expectedSalary)
    {
        ArgumentNullException.ThrowIfNull(expectedSalary, nameof(expectedSalary));
        ExpectedSalary = expectedSalary;
    }

    public void Screen(int score)
    {
        EnsureOpen();
        if (score < 0 || score > 100)
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 0 and 100.");
        ScreeningScore = score;
        Status = JobApplicationStatus.Screening;
    }

    public void Shortlist()
    {
        EnsureOpen();
        Status = JobApplicationStatus.Shortlisted;
    }

    public void ScheduleInterview(Interview interview)
    {
        ArgumentNullException.ThrowIfNull(interview, nameof(interview));
        EnsureOpen();
        _interviews.Add(interview);
        Status = JobApplicationStatus.Interviewing;
    }

    public void MakeOffer()
    {
        EnsureOpen();
        Status = JobApplicationStatus.OfferMade;
    }

    public void Hire()
    {
        if (Status != JobApplicationStatus.OfferMade)
            throw new InvalidOperationException("An offer must be made before the candidate can be hired.");
        Status = JobApplicationStatus.Hired;
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == JobApplicationStatus.Hired)
            throw new InvalidOperationException("A hired application cannot be rejected.");
        Status = JobApplicationStatus.Rejected;
        RejectionReason = reason.Trim();
    }

    public void Withdraw(string? reason = null)
    {
        if (Status == JobApplicationStatus.Hired)
            throw new InvalidOperationException("A hired application cannot be withdrawn.");
        Status = JobApplicationStatus.Withdrawn;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    private void EnsureOpen()
    {
        if (Status is JobApplicationStatus.Rejected or JobApplicationStatus.Withdrawn or JobApplicationStatus.Hired)
            throw new InvalidOperationException($"The application is {Status} and can no longer advance.");
    }
}
