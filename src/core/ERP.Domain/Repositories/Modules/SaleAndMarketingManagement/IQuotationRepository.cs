using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;
/// <summary>
/// Quotations - پیش‌فاکتور. Mostly asked about by time: what is waiting on an answer, and
/// what has quietly expired.
/// </summary>
public interface IQuotationRepository : IRepository<Quotation>
{
    Task<Quotation?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    /// <summary>A quotation with its lines, which is the only form in which its total is real.</summary>
    Task<Quotation?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Quotation>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Quotation>> GetByOpportunityAsync(Guid opportunityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quotations sent and not yet answered. These are what a salesperson chases, and the reason
    /// the list exists is that a quotation nobody follows up expires into a lost sale.
    /// </summary>
    Task<IReadOnlyList<Quotation>> GetAwaitingDecisionAsync(Guid? salesRepHumanId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sent quotations whose expiry has passed. Nothing expires by itself - a row stays Sent
    /// until somebody looks - so this is what a sweep reads to close them out.
    /// </summary>
    Task<IReadOnlyList<Quotation>> GetExpiredAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default);

    /// <summary>The highest version issued under one number, which a revision counts on from.</summary>
    Task<int> GetLatestVersionAsync(string number, CancellationToken cancellationToken = default);
}
