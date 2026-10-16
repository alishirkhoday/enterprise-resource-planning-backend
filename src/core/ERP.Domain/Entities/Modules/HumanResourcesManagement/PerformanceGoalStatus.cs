namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Whether a goal set in a review was met.
/// </summary>
public enum PerformanceGoalStatus
{
    Draft = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3
}
