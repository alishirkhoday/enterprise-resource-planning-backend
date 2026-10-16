namespace ERP.Domain.Entities.Users;

/// <summary>
/// Whether an account may be used. Distinct from a lockout, which expires by itself.
/// </summary>
public enum UserStatus
{
    Inactive = 0,
    Active = 1,
    Ban = 2
}
