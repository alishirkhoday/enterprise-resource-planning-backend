namespace ERP.Domain.Entities.Modules.ProjectManagement;

/// <summary>
/// Whether a milestone has been reached - مایلستون. Often the trigger for billing a stage,
/// which is why reaching one is a fact the rest of the system hears about.
/// </summary>
public enum MilestoneStatus
{
    Pending = 0,
    Achieved = 1,

    /// <summary>Reached, but after the date it was due.</summary>
    AchievedLate = 2,

    /// <summary>Invoiced to the customer.</summary>
    Billed = 3,

    Missed = 4
}
