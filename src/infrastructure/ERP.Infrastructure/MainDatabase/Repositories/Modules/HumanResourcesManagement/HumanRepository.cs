using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.HumanResourcesManagement;

public class HumanRepository(IMainDbContext context) : Repository<Human>(context), IHumanRepository
{
    // Both identifiers are optional on the employee record, because not everyone holds a
    // passport and a new starter may be registered before their papers arrive. The null check
    // is what keeps the comparison from matching rows that have no identifier at all.
    public async Task<bool> IsExistenceByNationalIdAsync(string nationalId, CancellationToken cancellationToken = default)
    {
        return await Query.AnyAsync(h => h.NationalId != null && h.NationalId.Value == nationalId, cancellationToken);
    }

    public async Task<bool> IsExistenceByPassportIdAsync(string passportId, CancellationToken cancellationToken = default)
    {
        return await Query.AnyAsync(h => h.PassportId != null && h.PassportId.Value == passportId, cancellationToken);
    }

    public async Task<Human?> GetByEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(h => h.EmployeeNumber == employeeNumber, cancellationToken);
    }

    public async Task<Human?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(h => h.UserId == userId, cancellationToken);
    }

    public async Task<Human?> GetWithContractsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(h => h.Contracts).ThenInclude(c => c.Position)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Human>> GetActiveAsync(Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(h => h.Status == HumanStatus.Active && (branchId == null || h.BranchId == branchId))
            .OrderBy(h => h.Family).ThenBy(h => h.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Human>> GetDirectReportsAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(h => h.ManagerId == managerId && h.Status == HumanStatus.Active)
            .OrderBy(h => h.Family).ThenBy(h => h.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Position?> GetPositionAsync(Guid positionId, CancellationToken cancellationToken = default)
    {
        return await _context.Positions.FirstOrDefaultAsync(p => p.Id == positionId, cancellationToken);
    }
}
