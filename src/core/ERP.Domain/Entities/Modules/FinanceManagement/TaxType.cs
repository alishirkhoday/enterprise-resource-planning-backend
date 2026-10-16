namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// What kind of tax a line carries - value added, withholding and the rest.
/// </summary>
public enum TaxType
{
    /// <summary>Value added tax charged on a sale.</summary>
    ValueAdded = 0,

    /// <summary>The municipal levy charged alongside value added tax.</summary>
    Municipal = 1,

    /// <summary>Tax withheld from a payment on behalf of the recipient.</summary>
    Withholding = 2,

    /// <summary>Tax on employment income.</summary>
    PayrollIncome = 3,

    /// <summary>Duty charged on imports.</summary>
    Customs = 4,

    Excise = 5,
    Other = 99
}
