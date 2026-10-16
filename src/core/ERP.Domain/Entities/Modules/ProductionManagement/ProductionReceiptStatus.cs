namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Whether finished goods have actually reached the shelf.
/// </summary>
public enum ProductionReceiptStatus
{
    Draft = 0,
    Confirmed = 1,
    Cancelled = 2
}
