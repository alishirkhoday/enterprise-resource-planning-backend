using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.DecideBudget
{
    public record DecideBudgetCommand : DecideBudgetDto, ICommand<Result<BudgetDto, Error>>;
}
