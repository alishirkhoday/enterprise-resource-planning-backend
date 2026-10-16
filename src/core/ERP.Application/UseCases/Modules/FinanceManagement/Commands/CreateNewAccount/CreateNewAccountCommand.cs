using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.CreateNewAccount
{
    public record CreateNewAccountCommand : AddEditAccountDto, ICommand<Result<string, Error>>
    {
    }
}
