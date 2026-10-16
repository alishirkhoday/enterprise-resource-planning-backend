using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.CompleteActivity
{
    public record CompleteActivityCommand : CompleteActivityDto, ICommand<Result<CompleteActivityResultDto, Error>>;
}
