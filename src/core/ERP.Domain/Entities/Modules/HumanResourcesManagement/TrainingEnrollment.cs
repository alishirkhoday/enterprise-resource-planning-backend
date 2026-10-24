namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// One employee attending one session, and the certificate that results if they pass.
/// </summary>
public sealed class TrainingEnrollment : BaseEntity
{
    public TrainingSession Session { get; private set; } = default!;
    public Guid SessionId { get; private set; }

    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public DateTimeOffset EnrolledOn { get; private set; }
    public TrainingEnrollmentStatus Status { get; private set; }

    public decimal? AttendedHours { get; private set; }
    public int? Score { get; private set; }
    public bool Passed { get; private set; }

    public string? CertificateNumber { get; private set; }
    public DateOnly? CertificateIssuedOn { get; private set; }
    public DateOnly? CertificateExpiresOn { get; private set; }

    private TrainingEnrollment() { }

    public TrainingEnrollment(TrainingSession session, Human human)
    {
        ArgumentNullException.ThrowIfNull(session, nameof(session));
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        Session = session;
        SessionId = session.Id;
        Human = human;
        HumanId = human.Id;
        EnrolledOn = DateTimeOffset.UtcNow;
        Status = TrainingEnrollmentStatus.Enrolled;
    }

    internal void RecordAttendance(decimal attendedHours)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(attendedHours, nameof(attendedHours));
        AttendedHours = attendedHours;
        Status = TrainingEnrollmentStatus.Attended;
    }

    internal void RecordResult(int score, int passingScore = 60)
    {
        if (score < 0 || score > 100)
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 0 and 100.");
        Score = score;
        Passed = score >= passingScore;
        Status = Passed ? TrainingEnrollmentStatus.Passed : TrainingEnrollmentStatus.Failed;
    }

    internal void IssueCertificate(string certificateNumber, DateOnly issuedOn, int? validityMonths)
    {
        if (!Passed)
            throw new InvalidOperationException("A certificate can only be issued to an employee who passed.");
        ArgumentException.ThrowIfNullOrWhiteSpace(certificateNumber, nameof(certificateNumber));
        var expiresOn = validityMonths.HasValue ? (DateOnly?)issuedOn.AddMonths(validityMonths.Value) : null;
        CertificateNumber = certificateNumber.Trim();
        CertificateIssuedOn = issuedOn;
        CertificateExpiresOn = expiresOn;
    }

    internal void MarkAsAbsent() => Status = TrainingEnrollmentStatus.Absent;

    internal void Cancel() => Status = TrainingEnrollmentStatus.Cancelled;

    public bool IsCertificateValidOn(DateOnly date) =>
        CertificateIssuedOn is not null && (CertificateExpiresOn is null || CertificateExpiresOn.Value >= date);
}
