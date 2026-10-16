using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.GrantLeaveEntitlement
{
    /// <summary>
    /// Opens somebody's leave balance for a year - تخصیص مرخصی.
    /// <para>
    /// This is the piece the leave rules were resting on and nobody had built: a request checks
    /// itself against the balance, and until now the balance could only be seeded by hand. An
    /// employee with no row was simply refused, which reads to them as a bug.
    /// </para>
    /// <para>
    /// An existing balance is restated rather than replaced, so days already used or held stay
    /// where they are. Wiping them would hand back leave somebody has already taken.
    /// </para>
    /// </summary>
    public class GrantLeaveEntitlementCommandHandler(
        IHumanRepository humans,
        ILeaveRepository leave)
        : IRequestHandler<GrantLeaveEntitlementCommand, Result<LeaveEntitlementDto, Error>>
    {
        public async Task<Result<LeaveEntitlementDto, Error>> Handle(GrantLeaveEntitlementCommand request, CancellationToken cancellationToken)
        {
            var human = await humans.GetByIdAsync(request.HumanId, cancellationToken);
            if (human is null)
                return Result.Failure<LeaveEntitlementDto, Error>(Errors.General.NotFound(request.HumanId.ToString(), nameof(Human)));

            if (human.Status != HumanStatus.Active)
                return Result.Failure<LeaveEntitlementDto, Error>(Errors.HumanResources.EmployeeIsTerminated(human.FullName));

            var existing = await leave.GetEntitlementAsync(request.HumanId, request.LeaveType, request.Year, cancellationToken);

            try
            {
                if (existing is not null)
                {
                    // Restated, not replaced. Days already used or held against this balance are
                    // facts, and a new annual allowance does not undo them.
                    existing.SetEntitledDays(request.EntitledDays);

                    if (request.CarriedOverDays > 0m)
                        existing.CarryOver(request.CarriedOverDays);

                    // No update call: the entitlement came back tracked, so the change is picked
                    // up when the pipeline commits, exactly as deciding a leave request does.
                    return Result.Success<LeaveEntitlementDto, Error>(ToDto(existing, human));
                }

                var entitlement = new LeaveEntitlement(human, request.LeaveType, request.Year, request.EntitledDays);

                if (request.CarriedOverDays > 0m)
                    entitlement.CarryOver(request.CarriedOverDays);

                await leave.AddEntitlementAsync(entitlement, cancellationToken);

                return Result.Success<LeaveEntitlementDto, Error>(ToDto(entitlement, human));
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Result.Failure<LeaveEntitlementDto, Error>(Errors.HumanResources.LeaveCannotBeGranted(exception.Message));
            }
        }

        private static LeaveEntitlementDto ToDto(LeaveEntitlement entitlement, Human human) => new(
            entitlement.Id,
            entitlement.HumanId,
            human.FullName,
            entitlement.LeaveType,
            entitlement.Year,
            entitlement.EntitledDays,
            entitlement.CarriedOverDays,
            entitlement.UsedDays,
            entitlement.PendingDays,
            entitlement.RemainingDays);
    }
}
