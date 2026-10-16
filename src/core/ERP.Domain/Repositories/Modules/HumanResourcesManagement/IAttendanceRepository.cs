using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Domain.Repositories.Modules.HumanResourcesManagement;

/// <summary>Clock records - حضور و غیاب.</summary>
public interface IAttendanceRepository : IRepository<HumanAttendanceRecord>
{
    /// <summary>
    /// The record for one person on one day, if the clock has already seen them. A day has one
    /// record that is opened on entry and closed on exit, not two.
    /// </summary>
    Task<HumanAttendanceRecord?> GetForDayAsync(Guid humanId, DateOnly date, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HumanAttendanceRecord>> GetBetweenAsync(Guid humanId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Everybody's records for one day, which is what a supervisor reads in the morning and what
    /// an absence report is built from.
    /// </summary>
    Task<IReadOnlyList<HumanAttendanceRecord>> GetForDateAsync(DateOnly date, Guid? branchId = null, CancellationToken cancellationToken = default);

    Task AddAsync(HumanAttendanceRecord record, CancellationToken cancellationToken = default);
}
