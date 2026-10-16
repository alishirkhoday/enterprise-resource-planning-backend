namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// How the customer paid.
/// </summary>
public enum PaymentMethod
{
    /// <summary>Paid through a payment gateway.</summary>
    Online = 0,

    /// <summary>Spread across a plan of instalments.</summary>
    Installment = 1,

    Cash = 2,
    CardReader = 3,
    BankTransfer = 4,
    Cheque = 5,

    /// <summary>Paid from the customer stored balance.</summary>
    Wallet = 6,

    /// <summary>Paid to the courier on delivery.</summary>
    CashOnDelivery = 7,

    /// <summary>Billed against the customer credit terms.</summary>
    Credit = 8
}
