namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// Whether the business is still willing to sell to them. A suspended or blacklisted customer
/// can still be credited - often that block is exactly why the goods came back.
/// </summary>
public enum CustomerStatus
{
    Active = 0,

    /// <summary>Registered but not yet approved to trade with.</summary>
    Pending = 1,

    /// <summary>Temporarily not sold to, normally over an unpaid balance.</summary>
    Suspended = 2,

    Blacklisted = 3,

    /// <summary>No longer trading with the organization.</summary>
    Closed = 4
}
