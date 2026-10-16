using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.RegisterCheque
{
    public record RegisterChequeCommand : RegisterChequeDto, ICommand<Result<ChequeDto, Error>>;
}
