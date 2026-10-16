using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.EarnCommission
{
    public record EarnCommissionCommand : EarnCommissionDto, ICommand<Result<CommissionDto, Error>>;
}
