namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// What moved a customer's wallet balance - کیف پول.
/// </summary>
public enum WalletTransactionType
{
    /// <summary>The customer added money.</summary>
    TopUp = 0,

    /// <summary>Money spent on an order.</summary>
    Purchase = 1,

    /// <summary>A refund credited back rather than paid out.</summary>
    RefundCredit = 2,

    /// <summary>Credit given as part of a promotion.</summary>
    PromotionalCredit = 3,

    /// <summary>Money paid out of the wallet to the customer.</summary>
    Withdrawal = 4,

    /// <summary>A manual correction.</summary>
    Adjustment = 5
}
