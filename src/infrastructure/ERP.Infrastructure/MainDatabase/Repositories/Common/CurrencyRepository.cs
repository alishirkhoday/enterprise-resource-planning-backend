using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Common;
using ERP.Domain.Repositories.Common;

namespace ERP.Infrastructure.MainDatabase.Repositories.Common;

public class CurrencyRepository(IMainDbContext context) : Repository<Currency>(context), ICurrencyRepository
{
    public async Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return await _entity.FirstOrDefaultAsync(c => c.Code == normalized, cancellationToken);
    }

    public async Task<Currency?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(c => c.IsBaseCurrency, cancellationToken);
    }

    public async Task<IReadOnlyList<Currency>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await Query.Where(c => c.IsActive).OrderBy(c => c.Code).ToListAsync(cancellationToken);
    }
}
