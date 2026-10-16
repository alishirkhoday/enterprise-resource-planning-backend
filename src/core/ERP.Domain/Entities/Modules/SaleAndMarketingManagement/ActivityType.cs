namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// What kind of activity it is - a call, a meeting, an email.
/// </summary>
public enum ActivityType
{
    Call = 0,
    Email = 1,
    Meeting = 2,
    Visit = 3,
    Demonstration = 4,
    FollowUp = 5,
    Task = 6,
    Note = 7
}
