namespace ERP.Domain.Entities.Security;

/// <summary>
/// A permission granted to a role. The join that makes permissions manageable in groups
/// rather than one user at a time.
/// </summary>
public sealed class RolePermission : BaseEntity
{
    public Role Role { get; private set; } = default!;
    public Guid RoleId { get; private set; }

    public Permission Permission { get; private set; } = default!;
    public Guid PermissionId { get; private set; }

    private RolePermission() { }

    public RolePermission(Role role, Permission permission)
    {
        ArgumentNullException.ThrowIfNull(role, nameof(role));
        ArgumentNullException.ThrowIfNull(permission, nameof(permission));
        Role = role;
        RoleId = role.Id;
        Permission = permission;
        PermissionId = permission.Id;
    }
}
