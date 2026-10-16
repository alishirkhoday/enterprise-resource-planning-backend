using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ScheduleActivity
{
    public record ScheduleActivityCommand : ScheduleActivityDto, ICommand<Result<ActivityDto, Error>>;
}
