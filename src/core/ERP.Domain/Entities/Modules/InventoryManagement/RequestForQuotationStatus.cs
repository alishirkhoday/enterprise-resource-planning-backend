namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Where a request for quotation has got to - استعلام بها.
/// </summary>
public enum RequestForQuotationStatus
{
    Draft = 0,
    Issued = 1,

    /// <summary>The response window has closed and offers are being compared.</summary>
    Closed = 2,

    Awarded = 3,
    Cancelled = 4
}
