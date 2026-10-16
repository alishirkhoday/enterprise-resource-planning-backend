namespace ERP.Domain.Entities.Users;

/// <summary>
/// Whether a sign-in is still live, and if not, how it ended.
/// </summary>
public enum UserSessionStatus
{
    Active = 1,
    Revoked = 2,
    Expired = 3
}
