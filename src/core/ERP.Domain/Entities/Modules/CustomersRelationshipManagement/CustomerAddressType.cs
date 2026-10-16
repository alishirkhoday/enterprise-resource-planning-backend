namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// What an address is used for - billing, shipping, registered office.
/// </summary>
public enum CustomerAddressType
{
    /// <summary>Where goods are delivered.</summary>
    Shipping = 0,

    /// <summary>Where the invoice is addressed, which the tax authority reads.</summary>
    Billing = 1,

    Both = 2
}
