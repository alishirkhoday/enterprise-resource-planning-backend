using ERP.Application.UseCases.Modules.FinanceManagement.Commands.DraftBudget;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.DecideBudget
{
    /// <summary>
    /// Approves or rejects a submitted budget - تأیید بودجه.
    /// <para>
    /// Approval is what turns a plan into a control: until it happens, the budget check has
    /// nothing to measure a spend against. A rejection sends it back to draft so the numbers can
    /// be reworked rather than the whole thing being raised again.
    /// </para>
    /// </summary>
    public class DecideBudgetCommandHandler(IBudgetRepository budgets)
        : IRequestHandler<DecideBudgetCommand, Result<BudgetDto, Error>>
    {
        public async Task<Result<BudgetDto, Error>> Handle(DecideBudgetCommand request, CancellationToken cancellationToken)
        {
            var budget = await budgets.GetWithLinesAsync(request.BudgetId, cancellationToken);
            if (budget is null)
                return Result.Failure<BudgetDto, Error>(Errors.General.NotFound(request.BudgetId.ToString(), nameof(Budget)));

            try
            {
                if (request.IsApproved)
                    budget.Approve(request.ApprovedBy);
                else
                    budget.Reject();
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<BudgetDto, Error>(Errors.Finance.BudgetCannotBeChanged(exception.Message));
            }

            await budgets.UpdateAsync(budget, cancellationToken);

            return Result.Success<BudgetDto, Error>(DraftBudgetCommandHandler.ToDto(budget));
        }
    }
}
