using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Domain.Entities.Branches;

/// <summary>
/// Which branches an employee works for. Deliberately not branch-scoped itself: the row that
/// says where somebody belongs cannot be hidden by the filter it feeds.
/// </summary>
public sealed class BranchHuman : AggregateRoot
{
    public Branch Branch { get; private set; } = default!;
    public Guid BranchId { get; private set; }
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    private BranchHuman() { }
}
