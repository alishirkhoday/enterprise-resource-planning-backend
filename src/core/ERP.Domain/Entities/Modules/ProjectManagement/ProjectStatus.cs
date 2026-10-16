namespace ERP.Domain.Entities.Modules.ProjectManagement;

/// <summary>
/// Where a project has got to.
/// </summary>
public enum ProjectStatus
{
    Draft = 0,
    Active = 1,
    OnHold = 2,
    Completed = 3,
    Cancelled = 4
}
