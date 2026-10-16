namespace ERP.Domain.Entities.Security;

/// <summary>
/// A named bundle of permissions that users are assigned to.
/// </summary>
public sealed class Role : BaseEntity
{
    public string Title { get; private set; } = default!;
    public string NormalizedTitle { get; private set; } = default!;
    public string? Description { get; private set; }

    /// <summary>Seeded by the product; users must not be able to delete it.</summary>
    public bool IsSystematic { get; private set; }

    public bool IsActive { get; private set; }

    private readonly List<RolePermission> _rolePermissions = [];
    public IReadOnlyList<RolePermission> RolePermissions => _rolePermissions;

    public IEnumerable<Permission> Permissions => _rolePermissions.Select(rp => rp.Permission);

    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyList<UserRole> UserRoles => _userRoles;

    private Role() { }

    public Role(string title, bool isSystematic = false)
    {
        SetTitle(title);
        IsSystematic = isSystematic;
        IsActive = true;
    }

    public void SetTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        Title = title.Trim();
        NormalizedTitle = Normalization.NormalizeUsername(Title);
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void GrantPermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission, nameof(permission));
        if (_rolePermissions.Any(rp => rp.PermissionId == permission.Id))
            return;
        _rolePermissions.Add(new RolePermission(this, permission));
    }

    public void RevokePermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission, nameof(permission));
        var rolePermission = _rolePermissions.FirstOrDefault(rp => rp.PermissionId == permission.Id);
        if (rolePermission is not null)
            _rolePermissions.Remove(rolePermission);
    }

    public bool HasPermission(string permissionKey) =>
        _rolePermissions.Any(rp => string.Equals(rp.Permission.Key, permissionKey, StringComparison.OrdinalIgnoreCase));

    public void Activate() => IsActive = true;

    public void Deactivate()
    {
        if (IsSystematic)
            throw new InvalidOperationException("A systematic role cannot be deactivated.");
        IsActive = false;
    }
}
