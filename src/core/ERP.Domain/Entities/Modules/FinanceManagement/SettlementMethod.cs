namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// How money moved. Offset and advance are here too, because clearing one debt against
/// another and taking money before anything is owed both settle accounts without cash moving.
/// </summary>
public enum SettlementMethod
{
    Cash = 0,
    BankTransfer = 1,
    Cheque = 2,
    PaymentGateway = 3,
    PointOfSale = 4,

    /// <summary>Clears a receivable against a payable for the same party.</summary>
    Offset = 5,

    /// <summary>Money taken before anything is owed, held against the party account.</summary>
    Advance = 6
}
