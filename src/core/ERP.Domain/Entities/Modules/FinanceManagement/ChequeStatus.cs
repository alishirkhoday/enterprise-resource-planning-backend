namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Where a cheque is in its life - چک. What a cheque is worth depends entirely on this: one
/// in hand is an asset, one bounced is a debt that came back.
/// </summary>
public enum ChequeStatus
{
    /// <summary>Held by the organization and not yet acted on.</summary>
    InHand = 0,

    /// <summary>Handed to the bank and awaiting clearance.</summary>
    InCollection = 1,

    /// <summary>Handed to its payee, for a cheque the organization wrote.</summary>
    Issued = 2,

    Cleared = 3,
    Bounced = 4,

    /// <summary>Passed on to a third party in settlement of a debt.</summary>
    Endorsed = 5,

    /// <summary>Taken back from the bank after bouncing.</summary>
    Returned = 6,

    Cancelled = 7
}
