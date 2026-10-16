namespace ERP.Domain.Entities.Modules.PlanningManagement;

/// <summary>
/// How the figures were arrived at. Recorded because it is the first thing anyone asks when a
/// forecast turns out badly wrong.
/// </summary>
public enum ForecastMethod
{
    /// <summary>Entered by hand from judgement.</summary>
    Manual = 0,

    MovingAverage = 1,
    ExponentialSmoothing = 2,

    /// <summary>Last year's actuals, optionally scaled.</summary>
    SameperiodLastYear = 3,

    /// <summary>Derived from confirmed sales contracts and open quotations.</summary>
    CommittedDemand = 4,

    /// <summary>Produced outside this system and imported.</summary>
    External = 5
}
