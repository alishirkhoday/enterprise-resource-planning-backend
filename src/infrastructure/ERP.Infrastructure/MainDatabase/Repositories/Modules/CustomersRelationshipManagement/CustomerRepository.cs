using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.CustomersRelationshipManagement;

public class CustomerRepository(IMainDbContext context) : Repository<Customer>(context), ICustomerRepository
{
    public async Task<Customer?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
    }

    public async Task<Customer?> GetByNationalIdAsync(string nationalId, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(
            c => c.NationalId != null && c.NationalId.Value == nationalId,
            cancellationToken);
    }

    public async Task<Customer?> GetByEconomicCodeAsync(string economicCode, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(
            c => c.EconomicCode != null && c.EconomicCode.Value == economicCode,
            cancellationToken);
    }

    public async Task<Customer?> GetWithAddressesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Customer?> GetWithContactsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(c => c.Contacts)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> GetSellableAsync(Guid? customerGroupId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(c => c.Status == CustomerStatus.Active
                        && (customerGroupId == null || c.CustomerGroupId == customerGroupId))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsExistenceByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await Query.AnyAsync(
            c => c.Code == code && (excludeId == null || c.Id != excludeId),
            cancellationToken);
    }

    /// <summary>
    /// Joins the customer to what it still owes on the ledger. The balance lives on the
    /// receivables, not on the customer row, so the comparison has to be made across both.
    /// </summary>
    public async Task<IReadOnlyList<Customer>> GetOverCreditLimitAsync(CancellationToken cancellationToken = default)
    {
        var openStatuses = new[] { ReceivableStatus.Open, ReceivableStatus.PartiallySettled };

        var query = from customer in Query
                    where customer.CreditLimit != null && customer.DetailAccountId != null
                    let outstanding = _context.Receivables
                        .Where(r => r.PartyDetailAccountId == customer.DetailAccountId
                                    && openStatuses.Contains(r.Status))
                        .Sum(r => r.OriginalAmount.Amount - r.SettledAmount.Amount)
                    where outstanding > customer.CreditLimit!.Amount
                    orderby customer.Name
                    select customer;

        return await query.ToListAsync(cancellationToken);
    }
}
