using ERP.Domain.Entities.Users;

namespace ERP.Domain.Entities.Security;

/// <summary>
/// Assigns a role to a user, optionally only within one branch and only for a period.
/// This is the link that was missing between users and what they are allowed to do.
/// </summary>
public sealed class UserRole : BaseEntity
{
    public User User { get; private set; } = default!;
    public Guid UserId { get; private set; }

    public Role Role { get; private set; } = default!;
    public Guid RoleId { get; private set; }

    /// <summary>When set, the role only applies while the user acts within that branch.</summary>
    public Guid? BranchId { get; private set; }

    public DateTimeOffset AssignedOn { get; private set; }
    public DateTimeOffset? ExpiresOn { get; private set; }
    public string? AssignedBy { get; private set; }

    private UserRole() { }

    public UserRole(User user, Role role, Guid? branchId = null, DateTimeOffset? expiresOn = null)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        ArgumentNullException.ThrowIfNull(role, nameof(role));
        if (expiresOn.HasValue && expiresOn.Value <= DateTimeOffset.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expiresOn), "ExpiresOn must be in the future.");

        User = user;
        UserId = user.Id;
        Role = role;
        RoleId = role.Id;
        BranchId = branchId == Guid.Empty ? null : branchId;
        AssignedOn = DateTimeOffset.UtcNow;
        ExpiresOn = expiresOn;
    }

    internal void SetAssignedBy(string? assignedBy) => AssignedBy = assignedBy;

    internal void Extend(DateTimeOffset expiresOn)
    {
        if (expiresOn <= DateTimeOffset.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expiresOn), "ExpiresOn must be in the future.");
        ExpiresOn = expiresOn;
    }

    internal void MakePermanent() => ExpiresOn = null;

    public bool IsCurrentlyEffective => !IsDeleted && (ExpiresOn is null || ExpiresOn.Value > DateTimeOffset.UtcNow);
}
