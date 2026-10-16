using ERP.Domain.Entities.Modules.ProductionManagement;

namespace ERP.Domain.Repositories.Modules.ProductionManagement;
/// <summary>
/// Machines on the shop floor, their state and when they are next due for service.
/// </summary>
public interface IMachineRepository : IRepository<Machine>
{
    Task<Machine?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Machine>> GetByStatusAsync(MachineStatus status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Machine>> GetByWorkCenterAsync(Guid workCenterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Machines that have run past their service interval since the last one. A machine does not
    /// change when it becomes due - the hours accumulated - so only a scan can notice.
    /// </summary>
    Task<IReadOnlyList<Machine>> GetDueForMaintenanceAsync(CancellationToken cancellationToken = default);
}
