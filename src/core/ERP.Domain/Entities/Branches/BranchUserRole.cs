using ERP.Domain.Entities.Security;
using ERP.Domain.Entities.Users;

namespace ERP.Domain.Entities.Branches;

/// <summary>
/// A role somebody holds in one particular branch. The same person can be a manager in one
/// branch and an ordinary user in another, which a role on the user alone cannot express.
/// </summary>
public sealed class BranchUserRole : BaseEntity
{
    public Branch Branch { get; private set; } = default!;
    public Guid BranchId { get; private set; }
    public User User { get; private set; } = default!;
    public Guid UserId { get; private set; }
    public Role Role { get; private set; } = default!;
    public Guid RoleId { get; private set; }

    private BranchUserRole() { }

    public BranchUserRole(Branch branch, User user, Role role)
    {
        ArgumentNullException.ThrowIfNull(branch, nameof(branch));
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        ArgumentNullException.ThrowIfNull(role, nameof(role));
        Branch = branch;
        BranchId = branch.Id;
        User = user;
        UserId = user.Id;
        Role = role;
        RoleId = role.Id;
    }
}
