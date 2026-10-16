namespace ERP.Domain.Entities.Modules.ProjectManagement;

/// <summary>
/// What non project time was spent on, so the gap between worked and billable hours is explainable.
/// </summary>
public enum TimesheetActivityType
{
    InternalMeeting = 0,
    Training = 1,
    Administration = 2,
    PreSales = 3,
    ResearchAndDevelopment = 4,
    Support = 5,
    Idle = 6,
    Other = 99
}
