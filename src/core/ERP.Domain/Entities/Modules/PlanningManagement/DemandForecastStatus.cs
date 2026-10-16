namespace ERP.Domain.Entities.Modules.PlanningManagement;

/// <summary>
/// Where a forecast has got to. Only an approved forecast should have anything committed
/// against it.
/// </summary>
public enum DemandForecastStatus
{
    Draft = 0,
    Approved = 1,

    /// <summary>Replaced by a newer forecast, but kept so earlier plans still explain themselves.</summary>
    Superseded = 2
}
