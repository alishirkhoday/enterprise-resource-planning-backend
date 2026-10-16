namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Where a supplier's answer to an enquiry has got to.
/// </summary>
public enum SupplierQuotationStatus
{
    Received = 0,
    Accepted = 1,
    Rejected = 2,
    Expired = 3
}
