namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// Where a commission has got to. Accrued is not owed: commission on an invoice the customer
/// has not paid is a provision against money that may never arrive.
/// </summary>
public enum CommissionStatus
{
    /// <summary>Earned on paper, normally pending customer payment.</summary>
    Accrued = 0,

    Approved = 1,
    Paid = 2,
    Reversed = 3
}
