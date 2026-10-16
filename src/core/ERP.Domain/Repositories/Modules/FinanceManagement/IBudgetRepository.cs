using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Domain.Repositories.Modules.FinanceManagement;
/// <summary>
/// Budgets and the lines that control spending per account and period - بودجه.
/// </summary>
public interface IBudgetRepository : IRepository<Budget>
{
    Task<Budget?> GetByTitleAsync(string title, Guid financialYearId, CancellationToken cancellationToken = default);

    Task<Budget?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Budget>> GetByFinancialYearAsync(Guid financialYearId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The budget line governing an account in a period, so a posting can be checked against it
    /// before it is made rather than explained afterwards.
    /// </summary>
    Task<BudgetLine?> GetLineForAsync(Guid financialYearId, Guid accountId, short periodNumber, Guid? costCenterId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lines where actual spending has passed what was planned. Nothing happens to a budget line
    /// when it tips over, so this is only ever found by looking.
    /// </summary>
    Task<IReadOnlyList<BudgetLine>> GetOverspentLinesAsync(Guid? financialYearId = null, CancellationToken cancellationToken = default);
}
