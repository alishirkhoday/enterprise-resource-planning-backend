using ERP.Domain.Entities.Modules.OrderManagement;

namespace ERP.Domain.Entities.Branches;

/// <summary>
/// Which branch an order was taken by, for the cases where the order itself is shared.
/// </summary>
public sealed class BranchOrder : AggregateRoot
{
    public Branch Branch { get; private set; } = default!;
    public Guid BranchId { get; private set; }
    public Order Order { get; private set; } = default!;
    public Guid OrderId { get; private set; }

    private BranchOrder() { }
}
