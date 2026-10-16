using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Modules.FinanceManagement;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.DraftBudget
{
    /// <summary>
    /// Draws up a budget for a financial year - بودجه.
    /// <para>
    /// Lines are per account per period, because that is the grain at which spending is actually
    /// controlled: an annual figure for a cost centre tells nobody in March whether they are
    /// ahead or behind.
    /// </para>
    /// </summary>
    public class DraftBudgetCommandHandler(
        IBudgetRepository budgets,
        IAccountRepository accounts,
        IFinancialYearRepository financialYears)
        : IRequestHandler<DraftBudgetCommand, Result<BudgetDto, Error>>
    {
        public async Task<Result<BudgetDto, Error>> Handle(DraftBudgetCommand request, CancellationToken cancellationToken)
        {
            var financialYear = await financialYears.GetByIdAsync(request.FinancialYearId, cancellationToken);
            if (financialYear is null)
                return Result.Failure<BudgetDto, Error>(Errors.General.NotFound(request.FinancialYearId.ToString(), nameof(FinancialYear)));

            var title = request.Title.Trim();

            var existing = await budgets.GetByTitleAsync(title, request.FinancialYearId, cancellationToken);
            if (existing is not null)
                return Result.Failure<BudgetDto, Error>(Errors.General.AlreadyExists($"budget '{title}'"));

            var found = await accounts.GetByIdsAsync([.. request.Lines.Select(l => l.AccountId)], cancellationToken);
            var byId = found.ToDictionary(a => a.Id);

            try
            {
                var budget = new Budget(request.FinancialYearId, title, request.Type, request.CurrencyCode.Trim().ToUpperInvariant());

                budget.ScopeTo(request.CostCenterId, request.BranchId, request.ProjectId);
                budget.Update(title, request.Description);

                foreach (var line in request.Lines)
                {
                    if (!byId.TryGetValue(line.AccountId, out var account))
                        return Result.Failure<BudgetDto, Error>(Errors.General.NotFound(line.AccountId.ToString(), nameof(Account)));

                    // Only a final account carries postings, so only a final account can be
                    // budgeted against: a group account would never accumulate an actual.
                    if (!account.IsFinal)
                        return Result.Failure<BudgetDto, Error>(Errors.Account.AccountIsNotFinal(account.Code));

                    var budgetLine = new BudgetLine(
                        budget,
                        account,
                        line.PeriodNumber,
                        Money.Create(line.PlannedAmount, budget.CurrencyCode));

                    if (line.CostCenterId is not null)
                        budgetLine.AssignCostCenter(line.CostCenterId.Value);

                    budgetLine.SetDescription(line.Description);
                    budget.AddLine(budgetLine);
                }

                if (request.SubmitImmediately)
                    budget.Submit();

                await budgets.CreateAsync(budget, cancellationToken);

                return Result.Success<BudgetDto, Error>(ToDto(budget));
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or ArgumentOutOfRangeException)
            {
                return Result.Failure<BudgetDto, Error>(Errors.Finance.BudgetCannotBeChanged(exception.Message));
            }
        }

        internal static BudgetDto ToDto(Budget budget) => new(
            budget.Id,
            budget.FinancialYearId,
            budget.Title,
            budget.Type,
            budget.Status,
            budget.CurrencyCode,
            budget.Version,
            budget.TotalPlanned().Amount,
            budget.TotalActual().Amount,
            budget.TotalVariance().Amount,
            budget.CostCenterId,
            budget.ProjectId,
            budget.ApprovedOn,
            budget.ApprovedBy,
            [.. budget.Lines
                .OrderBy(l => l.PeriodNumber)
                .ThenBy(l => l.Account.Code)
                .Select(l => new BudgetLineViewDto(
                    l.Id,
                    l.AccountId,
                    l.Account.Code,
                    l.Account.Name,
                    l.PeriodNumber,
                    l.PlannedAmount.Amount,
                    l.ActualAmount.Amount,
                    l.CommittedAmount.Amount,
                    l.Available.Amount,
                    l.IsOverspent))]);
    }
}
