using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

namespace ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;
/// <summary>
/// Marketing campaigns - کمپین. What was spent, and what came back.
/// </summary>
public interface ICampaignRepository : IRepository<Campaign>
{
    Task<Campaign?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<Campaign?> GetWithLeadsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Campaigns running on a date, which is what a new lead can be attributed to.</summary>
    Task<IReadOnlyList<Campaign>> GetRunningOnAsync(DateOnly onDate, CancellationToken cancellationToken = default);

    Task<bool> IsExistenceByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
