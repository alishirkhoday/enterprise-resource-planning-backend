using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RequestLeave
{
    public record RequestLeaveCommand : RequestLeaveDto, ICommand<Result<LeaveRequestDto, Error>>;
}
