using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Queries.CheckBudget
{
    /// <summary>
    /// Whether a spend still fits - کنترل بودجه. Asked before committing to a purchase, not after.
    /// </summary>
    public sealed record CheckBudgetQuery : IQuery<Result<BudgetCheckDto, Error>>
    {
        public Guid FinancialYearId { get; init; }

        public Guid AccountId { get; init; }

        public short PeriodNumber { get; init; }

        public decimal Amount { get; init; }

        /// <summary>Narrows to a cost centre's own line, when the budget is split that way.</summary>
        public Guid? CostCenterId { get; init; }
    }
}
