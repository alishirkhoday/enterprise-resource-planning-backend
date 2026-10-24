using ERP.Domain.Entities.Security;

namespace ERP.Domain.Entities.Users;

public sealed partial class User
{
    public void SetUserActivityHistoryRequestContext(Guid childId, string? internetProtocolAddress, string? userAgent, string? deviceName)
    {
        var child = GetChild(childId, _activityHistories);
        child.SetRequestContext(internetProtocolAddress, userAgent, deviceName);
    }

    public void SetUserActivityHistoryDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _activityHistories);
        child.SetDescription(description);
    }

    public void SetUserActivityHistoryReference(Guid childId, string referenceType, string referenceId)
    {
        var child = GetChild(childId, _activityHistories);
        child.SetReference(referenceType, referenceId);
    }

    public void SetUserRoleAssignedBy(Guid childId, string? assignedBy)
    {
        var child = GetChild(childId, _roles);
        child.SetAssignedBy(assignedBy);
    }

    public void ExtendUserRole(Guid childId, DateTimeOffset expiresOn)
    {
        var child = GetChild(childId, _roles);
        child.Extend(expiresOn);
    }

    public void MakeUserRolePermanent(Guid childId)
    {
        var child = GetChild(childId, _roles);
        child.MakePermanent();
    }
}
