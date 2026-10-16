using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ApproveCommission
{
    public record ApproveCommissionCommand : ApproveCommissionDto, ICommand<Result<CommissionDto, Error>>;
}
