namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// One scheduled run of a <see cref="TrainingCourse"/>, with a capacity and a roster.
/// </summary>
public sealed class TrainingSession : BaseEntity
{
    public TrainingCourse Course { get; private set; } = default!;
    public Guid CourseId { get; private set; }

    public DateTimeOffset StartDateTime { get; private set; }
    public DateTimeOffset EndDateTime { get; private set; }
    public string? Location { get; private set; }
    public string? Instructor { get; private set; }

    /// <summary>Zero means there is no cap on attendance.</summary>
    public int Capacity { get; private set; }

    public TrainingSessionStatus Status { get; private set; }
    public Guid? CostCenterId { get; private set; }

    private readonly List<TrainingEnrollment> _enrollments = [];
    public IReadOnlyList<TrainingEnrollment> Enrollments => _enrollments;

    private TrainingSession() { }

    public TrainingSession(TrainingCourse course, DateTimeOffset startDateTime, DateTimeOffset endDateTime, int capacity = 0)
    {
        ArgumentNullException.ThrowIfNull(course, nameof(course));
        ArgumentOutOfRangeException.ThrowIfNegative(capacity, nameof(capacity));
        if (endDateTime <= startDateTime)
            throw new ArgumentOutOfRangeException(nameof(endDateTime), "EndDateTime must be after StartDateTime.");

        Course = course;
        CourseId = course.Id;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
        Capacity = capacity;
        Status = TrainingSessionStatus.Scheduled;
    }

    public void SetLogistics(string? location, string? instructor)
    {
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        Instructor = string.IsNullOrWhiteSpace(instructor) ? null : instructor.Trim();
    }

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void Reschedule(DateTimeOffset startDateTime, DateTimeOffset endDateTime)
    {
        if (Status is TrainingSessionStatus.Completed or TrainingSessionStatus.Cancelled)
            throw new InvalidOperationException($"A {Status} session cannot be rescheduled.");
        if (endDateTime <= startDateTime)
            throw new ArgumentOutOfRangeException(nameof(endDateTime), "EndDateTime must be after StartDateTime.");
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
    }

    public void Enroll(TrainingEnrollment enrollment)
    {
        ArgumentNullException.ThrowIfNull(enrollment, nameof(enrollment));
        if (Status != TrainingSessionStatus.Scheduled)
            throw new InvalidOperationException($"Enrolment is closed because the session is {Status}.");
        if (Capacity > 0 && _enrollments.Count >= Capacity)
            throw new InvalidOperationException("The session is full.");
        if (_enrollments.Any(e => e.HumanId == enrollment.HumanId))
            throw new InvalidOperationException("This employee is already enrolled in the session.");
        _enrollments.Add(enrollment);
    }

    public void Start() => Status = TrainingSessionStatus.InProgress;

    public void Complete() => Status = TrainingSessionStatus.Completed;

    public void Cancel()
    {
        if (Status == TrainingSessionStatus.Completed)
            throw new InvalidOperationException("A completed session cannot be cancelled.");
        Status = TrainingSessionStatus.Cancelled;
    }

    public int RemainingSeats => Capacity == 0 ? int.MaxValue : Capacity - _enrollments.Count;
}
