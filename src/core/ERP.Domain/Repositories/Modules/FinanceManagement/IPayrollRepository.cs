using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Domain.Repositories.Modules.FinanceManagement;
/// <summary>
/// Payslips - فیش حقوقی. The existence check for a period is the important one here: running
/// the same month twice is the most expensive mistake this module can make.
/// </summary>
public interface IPayrollRepository : IRepository<Payroll>
{
    /// <summary>A pay slip with its lines, which is the only form in which it adds up.</summary>
    Task<Payroll?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Payroll?> GetByHumanAndPeriodAsync(Guid humanId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    /// <summary>Whether this person has already been paid for this period, so a run cannot double pay.</summary>
    Task<bool> ExistsForPeriodAsync(Guid humanId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payroll>> GetByPeriodAsync(DateOnly startDate, DateOnly endDate, Guid? branchId = null, CancellationToken cancellationToken = default);

    /// <summary>Slips that are final but not yet paid, which is what a payment run picks up.</summary>
    Task<IReadOnlyList<Payroll>> GetUnpaidAsync(Guid? branchId = null, CancellationToken cancellationToken = default);
}
