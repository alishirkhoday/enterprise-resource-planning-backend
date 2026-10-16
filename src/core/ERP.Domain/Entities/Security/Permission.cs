namespace ERP.Domain.Entities.Security;

/// <summary>
/// A single thing a user is allowed to do. Permissions are granted through roles, never directly,
/// so revoking a role revokes everything it carried.
/// </summary>
public sealed class Permission : BaseEntity
{
    /// <summary>Stable machine key, for example Finance.Account.Create.</summary>
    public string Key { get; private set; } = default!;

    public string Title { get; private set; } = default!;

    /// <summary>The claim value written into the token.</summary>
    public string Value { get; private set; } = default!;

    /// <summary>The module the permission belongs to, used to group the permission tree.</summary>
    public string? Module { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Seeded by the product; users must not be able to delete it.</summary>
    public bool IsSystematic { get; private set; }

    private readonly List<RolePermission> _rolePermissions = [];
    public IReadOnlyList<RolePermission> RolePermissions => _rolePermissions;

    private Permission() { }

    public Permission(string key, string title, string value, string? module = null, bool isSystematic = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        Key = key.Trim();
        Title = title.Trim();
        Value = value.Trim();
        Module = string.IsNullOrWhiteSpace(module) ? null : module.Trim();
        IsSystematic = isSystematic;
    }

    public void Update(string title, string value, string? module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        Title = title.Trim();
        Value = value.Trim();
        Module = string.IsNullOrWhiteSpace(module) ? null : module.Trim();
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
