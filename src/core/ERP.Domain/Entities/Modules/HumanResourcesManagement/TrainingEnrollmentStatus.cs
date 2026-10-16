namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Whether somebody actually attended the course they were put on. Kept apart from the
/// session's own status, because a course that ran is not the same as a person who turned up.
/// </summary>
public enum TrainingEnrollmentStatus
{
    Enrolled = 0,
    Attended = 1,
    Passed = 2,
    Failed = 3,
    Absent = 4,
    Cancelled = 5
}
