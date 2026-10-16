namespace ERP.Domain.Entities.Modules.ProjectManagement;

/// <summary>
/// Where one task has got to.
/// </summary>
public enum ProjectTaskStatus
{
    NotStarted = 0,
    InProgress = 1,

    /// <summary>Stopped by something outside the task.</summary>
    Blocked = 2,

    Completed = 3,
    Cancelled = 4
}
