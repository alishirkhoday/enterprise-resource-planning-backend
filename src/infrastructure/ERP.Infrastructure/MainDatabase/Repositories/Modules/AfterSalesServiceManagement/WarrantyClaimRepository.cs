using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;
using ERP.Domain.Repositories.Modules.AfterSalesServiceManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.AfterSalesServiceManagement;

public class WarrantyClaimRepository(IMainDbContext context) : Repository<WarrantyClaim>(context), IWarrantyClaimRepository
{
    private static readonly WarrantyClaimStatus[] OpenStatuses =
    [
        WarrantyClaimStatus.Registered,
        WarrantyClaimStatus.UnderAssessment,
        WarrantyClaimStatus.Approved
    ];

    public async Task<WarrantyClaim?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(c => c.Number == number, cancellationToken);
    }

    public async Task<IReadOnlyList<WarrantyClaim>> GetOpenAsync(Guid? assignedToHumanId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(c => c.Customer)
            .Include(c => c.Item)
            .Where(c => OpenStatuses.Contains(c.Status)
                        && (assignedToHumanId == null || c.AssignedToHumanId == assignedToHumanId))
            .OrderBy(c => c.ReportedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WarrantyClaim>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(c => c.Item)
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.ReportedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WarrantyClaim>> GetBySerialNumberAsync(Guid serialNumberId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.SerialNumberId == serialNumberId)
            .OrderByDescending(c => c.FaultOccurredOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WarrantyClaim>> GetAwaitingSupplierRecoveryAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(c => c.Item)
            .Where(c => c.Status == WarrantyClaimStatus.Approved
                        && c.ClaimCost != null
                        && c.SupplierClaimReference == null)
            .OrderByDescending(c => c.ClaimCost!.Amount)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Grouped and summed in the database. The window is applied to when the fault happened rather
    /// than when it was reported, because a batch problem belongs to the period it was made in.
    /// </summary>
    public async Task<IReadOnlyList<WarrantyClaimRateRow>> GetClaimRateByItemAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.FaultOccurredOn >= from
                        && c.FaultOccurredOn <= to
                        && c.Status != WarrantyClaimStatus.Cancelled)
            .GroupBy(c => c.ItemId)
            .Select(g => new WarrantyClaimRateRow(
                g.Key,
                g.Count(),
                g.Count(c => c.Status == WarrantyClaimStatus.Approved || c.Status == WarrantyClaimStatus.Closed),
                g.Sum(c => c.ClaimCost != null ? c.ClaimCost.Amount : 0m)))
            .OrderByDescending(r => r.ClaimCount)
            .ToListAsync(cancellationToken);
    }
}
