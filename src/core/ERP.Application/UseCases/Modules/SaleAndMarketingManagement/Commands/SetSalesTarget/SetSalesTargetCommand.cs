using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.SetSalesTarget
{
    public record SetSalesTargetCommand : SetSalesTargetDto, ICommand<Result<SalesTargetDto, Error>>;
}
