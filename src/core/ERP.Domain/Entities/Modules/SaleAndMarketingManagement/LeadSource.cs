namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// Where a lead came from. What a channel is judged on when spend is compared with revenue.
/// </summary>
public enum LeadSource
{
    Website = 0,
    SocialMedia = 1,
    TradeShow = 2,
    PhoneCall = 3,
    Email = 4,
    Referral = 5
}
