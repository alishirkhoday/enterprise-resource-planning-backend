namespace ERP.Domain.Entities.Users;

/// <summary>
/// How a one-time code is delivered.
/// </summary>
public enum OneTimePasswordChannel
{
    SMS = 0,
    Email = 1
}
