using ERP.Application.UseCases.Modules.PlanningManagement.DTOs;

namespace ERP.Application.UseCases.Modules.PlanningManagement.Commands.ReleasePlannedOrder
{
    public record ReleasePlannedOrderCommand : ReleasePlannedOrderDto, ICommand<Result<ReleasePlannedOrderResultDto, Error>>;
}
