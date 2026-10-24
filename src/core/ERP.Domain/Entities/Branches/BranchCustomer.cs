using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Domain.Entities.Branches;

/// <summary>
/// Which branches a customer belongs to. A join table rather than a field on the customer,
/// because a customer can be shared between branches and often is.
/// </summary>
public sealed class BranchCustomer : AggregateRoot
{
    public Branch Branch { get; private set; } = default!;
    public Guid BranchId { get; private set; }
    public Customer Customer { get; private set; } = default!;
    public Guid CustomerId { get; private set; }

    private BranchCustomer() { }
}
