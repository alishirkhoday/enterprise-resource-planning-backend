namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Where a performance review has got to.
/// </summary>
public enum PerformanceReviewStatus
{
    Draft = 0,
    AwaitingAcknowledgement = 1,
    Finalized = 2
}
