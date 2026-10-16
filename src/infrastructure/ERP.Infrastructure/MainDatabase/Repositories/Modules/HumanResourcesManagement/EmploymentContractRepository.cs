using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.HumanResourcesManagement;

public class EmploymentContractRepository(IMainDbContext context) : Repository<HumanContract>(context), IEmploymentContractRepository
{
    /// <summary>
    /// The contract covering a date and not terminated before it. A person can have several over
    /// time, so payroll has to pick the one that was actually in force.
    /// </summary>
    public async Task<HumanContract?> GetEffectiveAsync(Guid humanId, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(c => c.Position)
            .Where(c => c.HumanId == humanId
                        && c.Period.StartDate <= onDate
                        && (c.Period.EndDate == null || c.Period.EndDate >= onDate)
                        && (c.TerminationDate == null || c.TerminationDate >= onDate))
            .OrderByDescending(c => c.Period.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HumanContract>> GetForHumanAsync(Guid humanId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(c => c.Position)
            .Where(c => c.HumanId == humanId)
            .OrderByDescending(c => c.Period.StartDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// An open ended contract never expires, so it is excluded rather than treated as expiring
    /// at some default date. One already terminated is not going to lapse either.
    /// </summary>
    public async Task<IReadOnlyList<HumanContract>> GetExpiringBetweenAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await Query
            .Include(c => c.Human)
            .Include(c => c.Position)
            .Where(c => c.Period.EndDate != null
                        && c.Period.EndDate >= from
                        && c.Period.EndDate <= to
                        && c.TerminationDate == null)
            .OrderBy(c => c.Period.EndDate)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(HumanContract contract, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contract, nameof(contract));
        _entity.Add(contract);
        return Task.CompletedTask;
    }
}
