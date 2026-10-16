using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.FinanceManagement;

public class FixedAssetRepository(IMainDbContext context) : Repository<FixedAsset>(context), IFixedAssetRepository
{
    public async Task<FixedAsset?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(a => a.Code == code, cancellationToken);
    }

    public async Task<FixedAsset?> GetWithDepreciationEntriesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(a => a.Category)
            .Include(a => a.DepreciationEntries)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <summary>
    /// In service, still above its salvage value, and with nothing already posted for the period.
    /// The last condition is what stops a second run in the same month charging twice.
    /// </summary>
    public async Task<IReadOnlyList<FixedAsset>> GetDueForDepreciationAsync(DateOnly periodEnd, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(a => a.Category)
            .Where(a => a.Status == FixedAssetStatus.InService
                        && a.DepreciationMethod != DepreciationMethod.None
                        && a.DepreciationStartDate != null
                        && a.DepreciationStartDate <= periodEnd
                        && a.AcquisitionCost.Amount - a.AccumulatedDepreciation.Amount > a.SalvageValue.Amount
                        && !a.DepreciationEntries.Any(e => e.Period.EndDate == periodEnd))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FixedAsset>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(a => a.CategoryId == categoryId)
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FixedAsset>> GetByCustodianAsync(Guid humanId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(a => a.CustodianHumanId == humanId && a.Status != FixedAssetStatus.Disposed)
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasDepreciationForPeriodAsync(Guid assetId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default)
    {
        return await _context.DepreciationEntries
            .AsNoTracking()
            .AnyAsync(e => e.AssetId == assetId
                           && e.Period.StartDate <= periodEnd
                           && (e.Period.EndDate == null || e.Period.EndDate >= periodStart),
                cancellationToken);
    }
}
