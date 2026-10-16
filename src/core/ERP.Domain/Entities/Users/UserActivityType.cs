namespace ERP.Domain.Entities.Users;

/// <summary>
/// What kind of thing the user did, for the security history.
/// </summary>
public enum UserActivityType
{
    SignIn = 0,
    SignOut = 1,
    SignUp = 2,
    PasswordChanged = 3,
    PasswordResetRequested = 4,
    VerificationCodeSent = 5,
    VerificationSucceeded = 6,
    VerificationFailed = 7,
    TwoFactorEnabled = 8,
    TwoFactorDisabled = 9,
    ProfileUpdated = 10,
    RoleAssigned = 11,
    RoleRevoked = 12,
    AccountLocked = 13,
    AccountBanned = 14,
    SessionRevoked = 15,
    PermissionDenied = 16
}
