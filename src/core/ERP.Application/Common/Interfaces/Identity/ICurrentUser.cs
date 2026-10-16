namespace ERP.Application.Common.Interfaces.Identity;

/// <summary>
/// Who is making the current request. Read by the auditing interceptor, which is why it exists at
/// all: the audit columns were on every entity from the start and never populated, because nothing
/// below the web layer knew a user existed.
/// <para>
/// Nullable throughout, because plenty of work has no user behind it - a migration, a background
/// job draining the outbox, a health check. Those write no name rather than an invented one.
/// </para>
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    /// <summary>What goes in the audit columns. Short, stable, and readable in a report.</summary>
    string? UserName { get; }

    bool IsAuthenticated { get; }
}
