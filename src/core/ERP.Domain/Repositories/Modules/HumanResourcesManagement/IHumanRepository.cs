using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Domain.Repositories.Modules.HumanResourcesManagement;

/// <summary>
/// The person and where they sit in the organisation.
/// <para>
/// Employment contracts, attendance and leave each have a repository of their own. They were all
/// here once, which made this the one contract every human resources handler had to take even to
/// clock somebody in, and made it impossible to tell from a constructor what a handler actually
/// touched.
/// </para>
/// </summary>
public interface IHumanRepository : IRepository<Human>
{
    Task<bool> IsExistenceByNationalIdAsync(string nationalId, CancellationToken cancellationToken = default);
    Task<bool> IsExistenceByPassportIdAsync(string passportId, CancellationToken cancellationToken = default);

    Task<Human?> GetByEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken = default);
    Task<Human?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>The employee with the contracts that say what they are paid and where they sit.</summary>
    Task<Human?> GetWithContractsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Human>> GetActiveAsync(Guid? branchId = null, CancellationToken cancellationToken = default);

    /// <summary>Who reports to this person, which is one level of the organization chart.</summary>
    Task<IReadOnlyList<Human>> GetDirectReportsAsync(Guid managerId, CancellationToken cancellationToken = default);

    Task<Position?> GetPositionAsync(Guid positionId, CancellationToken cancellationToken = default);
}
