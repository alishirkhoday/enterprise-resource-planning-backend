namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// A segment customers are grouped into, so pricing and terms can be set once for the whole group.
/// </summary>
public sealed class CustomerGroup : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    public Guid? DefaultPriceListId { get; private set; }
    public Guid? DefaultPaymentTermId { get; private set; }
    public Percentage? DefaultDiscountPercentage { get; private set; }

    public CustomerGroup? Parent { get; private set; }
    public Guid? ParentId { get; private set; }

    private readonly List<CustomerGroup> _children = [];
    public IReadOnlyList<CustomerGroup> Children => _children;

    public bool IsActive { get; private set; }

    private CustomerGroup() { }

    public CustomerGroup(string code, string name, CustomerGroup? parent = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Code = code.Trim();
        Name = name.Trim();
        Parent = parent;
        ParentId = parent?.Id;
        IsActive = true;
    }

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetDefaults(Guid? priceListId, Guid? paymentTermId, Percentage? discountPercentage)
    {
        DefaultPriceListId = priceListId == Guid.Empty ? null : priceListId;
        DefaultPaymentTermId = paymentTermId == Guid.Empty ? null : paymentTermId;
        DefaultDiscountPercentage = discountPercentage;
    }

    public void SetParent(CustomerGroup? parent)
    {
        if (parent is not null && parent.Id == Id)
            throw new InvalidOperationException("A group cannot be its own parent.");
        Parent = parent;
        ParentId = parent?.Id;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
