namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Why a reconciliation line did not match: timing, an error on one side, or something one
/// side never knew about.
/// </summary>
public enum BankReconciliationItemType
{
    /// <summary>Written and recorded, but not yet presented to the bank.</summary>
    OutstandingCheque = 0,

    /// <summary>Recorded as received, but not yet credited by the bank.</summary>
    DepositInTransit = 1,

    /// <summary>A fee the bank took that the ledger has not recorded.</summary>
    BankCharge = 2,

    /// <summary>Interest the bank paid that the ledger has not recorded.</summary>
    BankInterest = 3,

    DirectDebit = 4,
    DirectCredit = 5,

    /// <summary>A mistake in the organization books.</summary>
    BookError = 6,

    /// <summary>A mistake on the bank side.</summary>
    BankError = 7
}
