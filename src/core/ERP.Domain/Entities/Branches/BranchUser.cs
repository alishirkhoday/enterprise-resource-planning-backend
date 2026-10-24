using ERP.Domain.Entities.Users;

namespace ERP.Domain.Entities.Branches;

/// <summary>
/// Which branches a user can see. This is the row the query filter is built from, which is
/// why it is one of the few tables the filter never applies to itself.
/// </summary>
public sealed class BranchUser : AggregateRoot
{
    public Branch Branch { get; private set; } = default!;
    public Guid BranchId { get; private set; }
    public User User { get; private set; } = default!;
    public Guid UserId { get; private set; }

    private BranchUser() { }
}
