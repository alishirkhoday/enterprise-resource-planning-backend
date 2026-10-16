using ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.GrantLeaveEntitlement
{
    public record GrantLeaveEntitlementCommand : GrantLeaveEntitlementDto, ICommand<Result<LeaveEntitlementDto, Error>>;
}
