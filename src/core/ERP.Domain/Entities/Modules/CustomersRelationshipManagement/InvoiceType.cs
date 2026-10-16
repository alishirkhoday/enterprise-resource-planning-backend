namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// What kind of invoice it is. A credit note is its own type rather than a negative invoice,
/// because the original has already been filed with the tax authority.
/// </summary>
public enum InvoiceType
{
    /// <summary>An ordinary sales invoice.</summary>
    Sales = 0,

    /// <summary>A quote in invoice form, which does not create a receivable.</summary>
    Proforma = 1,

    /// <summary>Reduces what the customer owes, as on a sales return.</summary>
    CreditNote = 2,

    /// <summary>Increases what the customer owes, as on an undercharge.</summary>
    DebitNote = 3,

    /// <summary>A bill for part of a larger contract.</summary>
    Progress = 4
}
