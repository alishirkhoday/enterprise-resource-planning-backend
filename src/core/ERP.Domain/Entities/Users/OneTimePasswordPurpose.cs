namespace ERP.Domain.Entities.Users;

/// <summary>
/// What a one-time code is for. Codes are scoped to a purpose so one issued to verify a phone
/// number cannot be replayed to reset a password.
/// </summary>
public enum OneTimePasswordPurpose
{
    /// <summary>Confirming a phone number or email address.</summary>
    Verification = 0,

    SignIn = 1,
    PasswordReset = 2,
    TwoFactorAuthentication = 3,

    /// <summary>Confirming a sensitive change, such as a new bank account.</summary>
    SensitiveOperation = 4
}
