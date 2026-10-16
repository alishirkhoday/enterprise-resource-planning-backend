namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Whether a vacancy is still open to applicants.
/// </summary>
public enum JobPostingStatus
{
    Draft = 0,
    Published = 1,
    OnHold = 2,
    Closed = 3,
    Cancelled = 4
}
