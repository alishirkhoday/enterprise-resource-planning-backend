namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// Where a campaign is in its life. Only a running campaign takes attribution for new leads.
/// </summary>
public enum CampaignStatus
{
    Planned = 0,
    Running = 1,
    Paused = 2,
    Completed = 3,
    Cancelled = 4
}
