using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;
/// <summary>
/// Leads - سرنخ. The top of the funnel, before anybody is a customer.
/// </summary>
public interface ILeadRepository : IRepository<Lead>
{
    /// <summary>
    /// A lead with the notes, interactions and opportunities hanging off it, which is the only
    /// form in which it can be worked: every decision about a lead is made from its history.
    /// </summary>
    Task<Lead?> GetWithHistoryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leads still worth working, optionally for one salesperson. Converted and disqualified ones
    /// are left out: a worklist that shows closed leads stops being a worklist.
    /// </summary>
    Task<IReadOnlyList<Lead>> GetOpenAsync(Guid? assignedToHumanId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lead>> GetByCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether this phone number is already on a lead. The same person filling in a web form
    /// twice is the commonest duplicate in a sales database, and the number is what catches it.
    /// </summary>
    Task<Lead?> GetByMobileNumberAsync(string mobilePhoneNumberWithRegionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// How many leads a campaign produced and how many of those became customers, counted in the
    /// database. This is the only honest measure of whether a campaign was worth running.
    /// </summary>
    Task<(int Total, int Converted)> GetCampaignConversionAsync(Guid campaignId, CancellationToken cancellationToken = default);
}
