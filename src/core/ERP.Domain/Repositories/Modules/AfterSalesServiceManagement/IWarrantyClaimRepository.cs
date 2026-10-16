using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

namespace ERP.Domain.Repositories.Modules.AfterSalesServiceManagement;
/// <summary>
/// Warranty claims - ادعای گارانتی. Kept apart from service orders because a claim has to be
/// assessed before any work is agreed to.
/// </summary>
public interface IWarrantyClaimRepository : IRepository<WarrantyClaim>
{
    Task<WarrantyClaim?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarrantyClaim>> GetOpenAsync(Guid? assignedToHumanId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarrantyClaim>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>Every claim ever raised against one unit, which is the history an engineer wants.</summary>
    Task<IReadOnlyList<WarrantyClaim>> GetBySerialNumberAsync(Guid serialNumberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Claims accepted against a supplier's parts where the cost has not been passed back yet.
    /// Money already spent that somebody else agreed to carry.
    /// </summary>
    Task<IReadOnlyList<WarrantyClaim>> GetAwaitingSupplierRecoveryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// How often an item comes back, over a window. A rate that moves is the earliest signal a
    /// batch or a supplier has gone wrong, well before anyone opens a quality investigation.
    /// </summary>
    Task<IReadOnlyList<WarrantyClaimRateRow>> GetClaimRateByItemAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}

/// <summary>Claim counts for one item over a window.</summary>
public sealed record WarrantyClaimRateRow(Guid ItemId, int ClaimCount, int ApprovedCount, decimal ApprovedCost)
{
    public decimal ApprovalRatio => ClaimCount == 0 ? 0m : (decimal)ApprovedCount / ClaimCount;
}
