using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Queries.CheckBudget
{
    /// <summary>
    /// Asks whether a spend still fits inside the budget - کنترل بودجه.
    /// <para>
    /// Read only on purpose. It answers the question a purchase requisition or an expense claim
    /// needs answered, and reserving the money is a separate decision taken by whoever approves
    /// the spend - otherwise every price check would quietly eat budget.
    /// </para>
    /// <para>
    /// Available is planned less actual less committed, so money already promised to a purchase
    /// order counts against the line before the invoice arrives. Without that a budget looks
    /// healthy right up to the month the invoices land.
    /// </para>
    /// </summary>
    public class CheckBudgetQueryHandler(IBudgetRepository budgets)
        : IRequestHandler<CheckBudgetQuery, Result<BudgetCheckDto, Error>>
    {
        public async Task<Result<BudgetCheckDto, Error>> Handle(CheckBudgetQuery request, CancellationToken cancellationToken)
        {
            var line = await budgets.GetLineForAsync(
                request.FinancialYearId,
                request.AccountId,
                request.PeriodNumber,
                request.CostCenterId,
                cancellationToken);

            if (line is null)
                return Result.Failure<BudgetCheckDto, Error>(Errors.Finance.BudgetLineNotFound(request.AccountId.ToString()));

            var requested = Money.Create(request.Amount, line.PlannedAmount.CurrencyCode);

            return Result.Success<BudgetCheckDto, Error>(new BudgetCheckDto(
                request.FinancialYearId,
                line.AccountId,
                line.PeriodNumber,
                line.PlannedAmount.CurrencyCode,
                line.PlannedAmount.Amount,
                line.ActualAmount.Amount,
                line.CommittedAmount.Amount,
                line.Available.Amount,
                request.Amount,
                line.CanAccommodate(requested),
                line.ActualAmount.Amount + line.CommittedAmount.Amount + request.Amount,
                line.IsOverspent));
        }
    }
}
