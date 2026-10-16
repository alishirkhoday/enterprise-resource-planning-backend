using ERP.Application.Common.Interfaces.Identity;

namespace ERP.Infrastructure.Services.Identity;

/// <summary>
/// The caller for work that no person asked for: a background job draining the outbox, a scheduled
/// scan, a migration.
/// <para>
/// It writes a name into the audit columns rather than leaving them empty, because "the outbox
/// processor changed this" is a useful answer and an empty column is not.
/// </para>
/// </summary>
public sealed class SystemUser(string name) : ICurrentUser
{
    public Guid? UserId => null;

    public string? UserName => name;

    public bool IsAuthenticated => false;
}
