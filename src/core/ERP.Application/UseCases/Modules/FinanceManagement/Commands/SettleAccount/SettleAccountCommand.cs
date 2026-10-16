using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;

namespace ERP.Application.UseCases.Modules.FinanceManagement.Commands.SettleAccount
{
    public record SettleAccountCommand : SettleAccountDto, ICommand<Result<SettlementDto, Error>>;
}
