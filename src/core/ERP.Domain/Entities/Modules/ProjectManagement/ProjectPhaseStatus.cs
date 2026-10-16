namespace ERP.Domain.Entities.Modules.ProjectManagement;

/// <summary>
/// Where one phase of a project has got to.
/// </summary>
public enum ProjectPhaseStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}
