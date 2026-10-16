using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.CustomersRelationshipManagement;

public class InvoiceRepository(IMainDbContext context) : Repository<Invoice>(context), IInvoiceRepository
{
    private static readonly InvoiceStatus[] UnpaidStatuses =
    [
        InvoiceStatus.Posted,
        InvoiceStatus.PartiallyPaid
    ];

    public async Task<Invoice?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(i => i.Number == number, cancellationToken);
    }

    public async Task<Invoice?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(i => i.Customer)
            .Include(i => i.Items).ThenInclude(item => item.Item)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetByCustomerAsync(Guid customerId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(i => i.CustomerId == customerId
                        && (from == null || i.IssueDate >= from)
                        && (to == null || i.IssueDate <= to))
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetUnpaidAsync(Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(i => UnpaidStatuses.Contains(i.Status) && (customerId == null || i.CustomerId == customerId))
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetOverdueAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(i => i.Customer)
            .Where(i => UnpaidStatuses.Contains(i.Status) && i.DueDate < asOf)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// A proforma is excluded because it creates no liability, and a draft because it has not been
    /// issued. Only what actually reached the ledger belongs on a return.
    /// </summary>
    public async Task<IReadOnlyList<Invoice>> GetForTaxReturnAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .Where(i => i.Type != InvoiceType.Proforma
                        && i.Status != InvoiceStatus.Draft
                        && i.Status != InvoiceStatus.Cancelled
                        && i.IssueDate >= from
                        && i.IssueDate <= to)
            .OrderBy(i => i.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetLastNumberAsync(InvoiceType type, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(i => i.Type == type)
            .OrderByDescending(i => i.Number)
            .Select(i => i.Number)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
