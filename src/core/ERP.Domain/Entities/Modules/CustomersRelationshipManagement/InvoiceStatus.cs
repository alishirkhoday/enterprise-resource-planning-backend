namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// Where an invoice has got to. Issued means the customer owes it; posted means the ledger
/// knows about it, which are not the same moment.
/// </summary>
public enum InvoiceStatus
{
    Draft = 0,

    /// <summary>Sent to the customer but not yet in the ledger.</summary>
    Issued = 1,

    /// <summary>In the ledger, with a receivable behind it.</summary>
    Posted = 2,

    PartiallyPaid = 3,
    Paid = 4,
    Cancelled = 5
}
