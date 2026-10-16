using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Domain.Repositories.Modules.FinanceManagement;
/// <summary>
/// Fixed assets and their depreciation history - دارایی ثابت.
/// </summary>
public interface IFixedAssetRepository : IRepository<FixedAsset>
{
    Task<FixedAsset?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<FixedAsset?> GetWithDepreciationEntriesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assets that should be depreciated for a period: in service, not yet written down to their
    /// salvage value, and with no entry already posted for that period.
    /// </summary>
    Task<IReadOnlyList<FixedAsset>> GetDueForDepreciationAsync(DateOnly periodEnd, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FixedAsset>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FixedAsset>> GetByCustodianAsync(Guid humanId, CancellationToken cancellationToken = default);

    /// <summary>Whether a period has already been depreciated, so it is not charged twice.</summary>
    Task<bool> HasDepreciationForPeriodAsync(Guid assetId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default);
}
