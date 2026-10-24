namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// An address belonging to a customer. Several per customer: where an invoice goes and where
/// goods go are routinely different places.
/// </summary>
public sealed class CustomerAddress : BaseEntity
{
    public Customer Customer { get; private set; } = default!;
    public Guid CustomerId { get; private set; }

    public string? Title { get; private set; }
    public Address Address { get; private set; } = default!;
    public CustomerAddressType Type { get; private set; }
    public bool IsDefault { get; private set; }

    private CustomerAddress() { }

    public CustomerAddress(Customer customer, Address address, CustomerAddressType type = CustomerAddressType.Both)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        Customer = customer;
        CustomerId = customer.Id;
        Address = address.Copy();
        Type = type;
    }

    internal void Update(Address address, CustomerAddressType type)
    {
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        Address = address.Copy();
        Type = type;
    }

    internal void SetTitle(string? title) => Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();

    internal void SetDefault() => IsDefault = true;
    internal void UnsetDefault() => IsDefault = false;
}
