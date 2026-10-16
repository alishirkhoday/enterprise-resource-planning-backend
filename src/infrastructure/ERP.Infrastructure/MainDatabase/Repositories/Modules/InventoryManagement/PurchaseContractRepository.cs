using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.InventoryManagement;

public class PurchaseContractRepository(IMainDbContext context) : Repository<PurchaseContract>(context), IPurchaseContractRepository
{
    public async Task<PurchaseContract?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(c => c.Number == number, cancellationToken);
    }

    public async Task<PurchaseContract?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(c => c.Supplier)
            .Include(c => c.Items).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <summary>
    /// Tracked: whoever asks for the agreed price is usually about to raise an order against the
    /// contract, which means recording consumption on it in the same unit of work.
    /// </summary>
    public async Task<PurchaseContract?> GetEffectiveForItemAsync(Guid supplierId, Guid itemId, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(c => c.Items)
            .Where(c => c.SupplierId == supplierId
                        && c.Status == PurchaseContractStatus.Active
                        && c.Period.StartDate <= onDate
                        && (c.Period.EndDate == null || c.Period.EndDate >= onDate)
                        && c.Items.Any(i => i.ItemId == itemId))
            .OrderByDescending(c => c.Period.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseContract>> GetActiveForSupplierAsync(Guid supplierId, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.SupplierId == supplierId
                        && c.Status == PurchaseContractStatus.Active
                        && c.Period.StartDate <= onDate
                        && (c.Period.EndDate == null || c.Period.EndDate >= onDate))
            .OrderBy(c => c.Period.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseContract>> GetDueForRenewalAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(c => c.Supplier)
            .Where(c => c.Status == PurchaseContractStatus.Active
                        && c.AutoRenews
                        && c.RenewalNoticeDays != null
                        && c.Period.EndDate != null
                        && c.Period.EndDate.Value.DayNumber - asOf.DayNumber <= c.RenewalNoticeDays)
            .OrderBy(c => c.Period.EndDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Projected into a record rather than returning entities, because the question is about the
    /// gap between two money columns and the caller does not need the contract graph to see it.
    /// </summary>
    public async Task<IReadOnlyList<PurchaseContractCommitmentRow>> GetUnderconsumedAsync(DateOnly asOf, decimal minimumRemainingRatio, CancellationToken cancellationToken = default)
    {
        var rows = await Query
            .Where(c => c.Status == PurchaseContractStatus.Active
                        && c.CommittedValue != null
                        && c.CommittedValue.Amount > 0m
                        && c.Period.StartDate <= asOf
                        && (c.Period.EndDate == null || c.Period.EndDate >= asOf)
                        && (c.CommittedValue.Amount - c.ConsumedValue.Amount) / c.CommittedValue.Amount >= minimumRemainingRatio)
            .OrderBy(c => c.Period.EndDate)
            .Select(c => new PurchaseContractCommitmentRow(
                c.Id,
                c.Number,
                c.SupplierId,
                c.CurrencyCode,
                c.CommittedValue!.Amount,
                c.ConsumedValue.Amount,
                c.Period.EndDate))
            .ToListAsync(cancellationToken);

        return rows;
    }
}
