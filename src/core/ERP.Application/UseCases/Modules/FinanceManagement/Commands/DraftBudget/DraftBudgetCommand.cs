using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.DraftBudget
{
    public record DraftBudgetCommand : DraftBudgetDto, ICommand<Result<BudgetDto, Error>>;
}
