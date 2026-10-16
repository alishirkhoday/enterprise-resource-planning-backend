namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Whether a training course is still going to run.
/// </summary>
public enum TrainingSessionStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}
