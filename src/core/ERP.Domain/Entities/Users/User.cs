using ERP.Domain.Entities.Security;
using ERP.Domain.Entities.Users.ValueObjects;

namespace ERP.Domain.Entities.Users;

/// <summary>
/// An account somebody signs in with.
/// 
/// Identity only. Who the person is - their name, their employment, what they are paid - lives
/// on Human; a user can exist with no employee behind it, and an employee can exist with no
/// login. Tying the two together would mean neither could exist without the other.
/// </summary>
public sealed partial class User : AggregateRoot
{
    public Username Username { get; private set; } = default!;
    public string NormalizedUsername { get; private set; } = default!;
    public PasswordHash PasswordHash { get; private set; } = default!;
    public DateTimeOffset? PasswordChangedDateTime { get; private set; }

    /// <summary>Forces a password change on the next successful sign in.</summary>
    public bool MustChangePassword { get; private set; }

    public MobilePhoneNumberRegionCode MobilePhoneNumberRegionCode { get; private set; } = default!;
    public MobilePhoneNumber MobilePhoneNumber { get; private set; } = default!;
    public string? NormalizedMobilePhoneNumber { get; private set; }
    public bool MobilePhoneNumberConfirmed { get; private set; }

    public Email Email { get; private set; } = default!;
    public string? NormalizedEmail { get; private set; }
    public bool EmailConfirmed { get; private set; }

    public UserStatus Status { get; private set; }
    public int AccessFailedCount { get; private set; }
    public bool LockoutEnabled { get; private set; }
    public DateTimeOffset? LockoutEnd { get; private set; }
    public string SecurityStamp { get; private set; } = default!;
    public DateTimeOffset? LastSignInDateTime { get; private set; }

    public bool TwoFactorAuthenticationEnabled { get; private set; }

    private readonly List<UserActivityHistory> _activityHistories = [];
    public IReadOnlyList<UserActivityHistory> ActivityHistories => _activityHistories.AsReadOnly();

    private readonly List<UserSession> _sessions = [];
    public IReadOnlyList<UserSession> Sessions => _sessions.AsReadOnly();

    private readonly List<UserToken> _tokens = [];
    public IReadOnlyList<UserToken> Tokens => _tokens.AsReadOnly();

    private readonly List<UserRole> _roles = [];
    public IReadOnlyList<UserRole> Roles => _roles.AsReadOnly();

    private User() { }

    public User(string username, string passwordHash)
    {
        SetUsername(username);
        SetPassword(passwordHash);
        Status = UserStatus.Inactive;
        LockoutEnabled = true;
        Raise(new UserRegistered(Id, NormalizedUsername));
    }

    public void SetUsername(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username, nameof(username));
        Username = Username.Create(username);
        NormalizedUsername = Normalization.NormalizeUsername(Username.Value);
    }

    /// <summary>
    /// Also used to set the first password during registration, which is not a change and must not
    /// announce itself as one - a "your password was changed" message to someone who just signed up
    /// reads like a break-in.
    /// </summary>
    public void SetPassword(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash, nameof(passwordHash));
        var replacesAnExistingPassword = PasswordHash is not null;

        PasswordHash = PasswordHash.Create(passwordHash);
        PasswordChangedDateTime = DateTimeOffset.UtcNow;
        MustChangePassword = false;
        AccessFailedCount = 0;
        LockoutEnd = null;
        SetSecurityStamp();

        if (replacesAnExistingPassword)
            Raise(new UserPasswordChanged(Id));
    }

    public void RequirePasswordChange() => MustChangePassword = true;

    public void SetMobilePhoneNumber(string mobilePhoneNumberRegionCode, string mobilePhoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mobilePhoneNumberRegionCode, nameof(mobilePhoneNumberRegionCode));
        ArgumentException.ThrowIfNullOrWhiteSpace(mobilePhoneNumber, nameof(mobilePhoneNumber));
        var regionCode = MobilePhoneNumberRegionCode.Create(mobilePhoneNumberRegionCode);
        var number = MobilePhoneNumber.Create(mobilePhoneNumber);
        var normalized = Normalization.NormalizeMobilePhoneNumber(regionCode.Value, number.Value);
        MobilePhoneNumberRegionCode = regionCode;
        MobilePhoneNumber = number;
        NormalizedMobilePhoneNumber = normalized;
        MobilePhoneNumberConfirmed = false;
    }

    public void ConfirmMobilePhoneNumber() => MobilePhoneNumberConfirmed = true;

    public void SetEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));
        Email = Email.Create(email);
        NormalizedEmail = Normalization.NormalizeEmail(Email.Value);
        EmailConfirmed = false;
    }

    public void ConfirmEmail() => EmailConfirmed = true;

    public void Active() => Status = UserStatus.Active;

    public void Ban()
    {
        Status = UserStatus.Ban;
        AccessFailedCount = 0;
        LockoutEnd = null;
        SetSecurityStamp();
    }

    public void HandleAccessFailed()
    {
        AccessFailedCount++;
        if (AccessFailedCount > 5)
        {
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
        }
        if (AccessFailedCount > 10)
        {
            Status = UserStatus.Ban;
            Raise(new UserBanned(Id, "Too many failed sign in attempts."));
        }
        else if (LockoutEnd.HasValue)
        {
            Raise(new UserLockedOut(Id, LockoutEnd.Value, AccessFailedCount));
        }
    }

    public void ResetLockout()
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
    }

    /// <summary>
    /// Where the sign-in came from is carried on the event rather than stored here, because the
    /// audit trail wants every sign-in and this entity only keeps the latest.
    /// </summary>
    public void RecordSignIn(string? internetProtocolAddress = null, string? deviceName = null)
    {
        LastSignInDateTime = DateTimeOffset.UtcNow;
        ResetLockout();
        Raise(new UserSignedIn(Id, internetProtocolAddress, deviceName));
    }

    public void ForceLogoutAllSessions()
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
        SetSecurityStamp();
    }

    private void SetSecurityStamp() => SecurityStamp = Guid.NewGuid().ToString("N");

    public void Enable2FA() => TwoFactorAuthenticationEnabled = true;
    public void Disable2FA() => TwoFactorAuthenticationEnabled = false;

    public void EnableLockout() => LockoutEnabled = true;
    public void DisableLockout() => LockoutEnabled = false;

    public bool IsLockedOut => LockoutEnabled && LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;

    public void AssignRole(Role role, Guid? branchId = null, DateTimeOffset? expiresOn = null)
    {
        ArgumentNullException.ThrowIfNull(role, nameof(role));
        if (_roles.Any(r => r.RoleId == role.Id && r.BranchId == branchId && r.IsCurrentlyEffective))
            return;
        _roles.Add(new UserRole(this, role, branchId, expiresOn));
        SetSecurityStamp();
        Raise(new UserRoleAssigned(Id, role.Id, branchId));
    }

    public void RevokeRole(Role role, Guid? branchId = null)
    {
        ArgumentNullException.ThrowIfNull(role, nameof(role));
        var assignment = _roles.FirstOrDefault(r => r.RoleId == role.Id && r.BranchId == branchId);
        if (assignment is null)
            return;
        _roles.Remove(assignment);
        SetSecurityStamp();
        Raise(new UserRoleRevoked(Id, role.Id, branchId));
    }

    public bool IsInRole(string roleTitle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleTitle, nameof(roleTitle));
        var normalized = Normalization.NormalizeUsername(roleTitle);
        return _roles.Any(r => r.IsCurrentlyEffective && r.Role.NormalizedTitle == normalized);
    }

    /// <summary>
    /// The distinct permissions the user holds right now, through every effective role assignment.
    /// </summary>
    public IEnumerable<Permission> EffectivePermissions() =>
        _roles.Where(r => r.IsCurrentlyEffective && r.Role.IsActive)
              .SelectMany(r => r.Role.Permissions)
              .DistinctBy(p => p.Id);

    public bool HasPermission(string permissionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionKey, nameof(permissionKey));
        return EffectivePermissions().Any(p => string.Equals(p.Key, permissionKey, StringComparison.OrdinalIgnoreCase));
    }
}
