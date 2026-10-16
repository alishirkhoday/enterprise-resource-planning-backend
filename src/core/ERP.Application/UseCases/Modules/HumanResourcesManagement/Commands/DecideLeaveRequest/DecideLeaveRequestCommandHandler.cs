using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.DecideLeaveRequest
{
    /// <summary>
    /// Approves or rejects a leave request, and settles the days that were held for it.
    /// <para>
    /// Reserved days have to go somewhere. Approval turns them into used days; a rejection or a
    /// cancellation gives them back. Deciding a request without doing one or the other leaves
    /// days held forever against leave nobody is going to take.
    /// </para>
    /// </summary>
    public class DecideLeaveRequestCommandHandler(ILeaveRepository leave)
        : IRequestHandler<DecideLeaveRequestCommand, Result<LeaveRequestDto, Error>>
    {
        public async Task<Result<LeaveRequestDto, Error>> Handle(DecideLeaveRequestCommand request, CancellationToken cancellationToken)
        {
            var leaveRequest = await leave.GetRequestAsync(request.RequestId, cancellationToken);
            if (leaveRequest is null)
                return Result.Failure<LeaveRequestDto, Error>(Errors.General.NotFound(request.RequestId.ToString(), nameof(HumanRequest)));

            if (leaveRequest.Status != HumanRequestStatus.Pending)
                return Result.Failure<LeaveRequestDto, Error>(
                    Errors.HumanResources.RequestIsAlreadyDecided(leaveRequest.Status.ToString()));

            var days = leaveRequest.EndDate.DayNumber - leaveRequest.StartDate.DayNumber + 1;
            var drawsOnEntitlement = leaveRequest.Type == HumanRequestType.LeaveDaily && leaveRequest.LeaveType is not null;

            var entitlement = drawsOnEntitlement
                ? await leave.GetEntitlementAsync(
                    leaveRequest.HumanId,
                    leaveRequest.LeaveType!.Value,
                    leaveRequest.StartDate.Year,
                    cancellationToken)
                : null;

            try
            {
                if (request.IsApproved)
                {
                    leaveRequest.SetApprove();

                    // Held days become spent days. The entitlement moves them across in one step,
                    // so there is no moment where they are counted twice or not at all.
                    entitlement?.Consume(days);
                }
                else
                {
                    leaveRequest.SetReject();
                    entitlement?.Release(days);
                }
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure<LeaveRequestDto, Error>(Errors.HumanResources.LeaveRequestIsInvalid(exception.Message));
            }

            return Result.Success<LeaveRequestDto, Error>(new LeaveRequestDto(
                leaveRequest.Id,
                leaveRequest.HumanId,
                leaveRequest.Type,
                leaveRequest.LeaveType,
                leaveRequest.StartDate,
                leaveRequest.EndDate,
                leaveRequest.Status,
                entitlement?.RemainingDays));
        }
    }
}
