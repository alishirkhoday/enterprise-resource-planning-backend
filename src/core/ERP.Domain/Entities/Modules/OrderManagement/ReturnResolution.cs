namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// What the customer gets: a refund, a replacement or a credit.
/// </summary>
public enum ReturnResolution
{
    Refund = 0,
    Replacement = 1,

    /// <summary>Credited to the customer wallet rather than paid back.</summary>
    StoreCredit = 2,

    Repair = 3
}
