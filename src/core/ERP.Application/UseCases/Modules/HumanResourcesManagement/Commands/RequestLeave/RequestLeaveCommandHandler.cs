using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.RequestLeave
{
    /// <summary>
    /// Raises a leave request and holds the days against the person's entitlement.
    /// <para>
    /// The days are reserved now, not on approval. Between asking and being answered they are
    /// neither free nor spent, and an entitlement that only counts approved leave lets somebody
    /// book the same week three times while the first request sits in a queue.
    /// </para>
    /// </summary>
    public class RequestLeaveCommandHandler(
        IHumanRepository humans,
        ILeaveRepository leave)
        : IRequestHandler<RequestLeaveCommand, Result<LeaveRequestDto, Error>>
    {
        public async Task<Result<LeaveRequestDto, Error>> Handle(RequestLeaveCommand request, CancellationToken cancellationToken)
        {
            var human = await humans.GetByIdAsync(request.HumanId, cancellationToken);
            if (human is null)
                return Result.Failure<LeaveRequestDto, Error>(Errors.General.NotFound(request.HumanId.ToString(), nameof(Human)));

            if (human.Status != HumanStatus.Active)
                return Result.Failure<LeaveRequestDto, Error>(Errors.HumanResources.EmployeeIsNotActive(human.FullName));

            var clashes = await leave.GetOverlappingRequestsAsync(request.HumanId, request.StartDate, request.EndDate, cancellationToken);
            if (clashes.Count > 0)
            {
                var clash = clashes[0];
                return Result.Failure<LeaveRequestDto, Error>(
                    Errors.HumanResources.LeaveOverlaps(clash.StartDate, clash.EndDate, clash.Status.ToString()));
            }

            HumanRequest leaveRequest;
            try
            {
                leaveRequest = new HumanRequest(
                    human,
                    request.Type,
                    request.StartDate,
                    request.EndDate,
                    request.StartTime,
                    request.EndTime,
                    request.LeaveType,
                    request.Reason);
            }
            catch (ArgumentException exception)
            {
                return Result.Failure<LeaveRequestDto, Error>(Errors.HumanResources.LeaveRequestIsInvalid(exception.Message));
            }

            decimal? remaining = null;

            // Only daily leave draws on an entitlement. Hourly leave, remote work and a missed
            // clock-in are all requests, but none of them spends a day of anyone's allowance.
            if (request.Type == HumanRequestType.LeaveDaily && request.LeaveType is not null)
            {
                var reserved = await ReserveEntitlementAsync(human, request, cancellationToken);
                if (reserved.IsFailure)
                    return Result.Failure<LeaveRequestDto, Error>(reserved.Error);

                remaining = reserved.Value;
            }

            await leave.AddRequestAsync(leaveRequest, cancellationToken);

            return Result.Success<LeaveRequestDto, Error>(new LeaveRequestDto(
                leaveRequest.Id,
                leaveRequest.HumanId,
                leaveRequest.Type,
                leaveRequest.LeaveType,
                leaveRequest.StartDate,
                leaveRequest.EndDate,
                leaveRequest.Status,
                remaining));
        }

        private async Task<Result<decimal, Error>> ReserveEntitlementAsync(
            Human human,
            RequestLeaveCommand request,
            CancellationToken cancellationToken)
        {
            var days = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;

            var entitlement = await leave.GetEntitlementAsync(
                human.Id,
                request.LeaveType!.Value,
                request.StartDate.Year,
                cancellationToken);

            if (entitlement is null)
                return Result.Failure<decimal, Error>(
                    Errors.HumanResources.NoLeaveEntitlement(human.FullName, request.LeaveType.Value.ToString(), request.StartDate.Year));

            try
            {
                entitlement.Reserve(days);
            }
            catch (InvalidOperationException)
            {
                return Result.Failure<decimal, Error>(
                    Errors.HumanResources.NotEnoughLeave(request.LeaveType.Value.ToString(), days, entitlement.RemainingDays));
            }

            return Result.Success<decimal, Error>(entitlement.RemainingDays);
        }
    }
}
