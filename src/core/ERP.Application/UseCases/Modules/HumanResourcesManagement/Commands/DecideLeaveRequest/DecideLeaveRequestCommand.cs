using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.DecideLeaveRequest
{
    public sealed record DecideLeaveRequestCommand : ICommand<Result<LeaveRequestDto, Error>>
    {
        public Guid RequestId { get; init; }

        public bool IsApproved { get; init; }

        public string? Reason { get; init; }
    }
}
