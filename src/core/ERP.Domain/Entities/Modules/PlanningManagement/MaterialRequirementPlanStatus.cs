namespace ERP.Domain.Entities.Modules.PlanningManagement;

/// <summary>
/// Where a planning run has got to. A superseded run computed its suggestions against a
/// position that has since moved, which is why acting on one is refused.
/// </summary>
public enum MaterialRequirementPlanStatus
{
    Running = 0,
    Completed = 1,

    /// <summary>Ended early. Whatever lines were written are kept, because they usually show why.</summary>
    Failed = 2,

    /// <summary>Overtaken by a newer run; its unreleased suggestions are no longer worth acting on.</summary>
    Superseded = 3
}
