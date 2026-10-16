namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A single step in a cheque's history. Recorded as operations rather than by overwriting the
/// status, because a bounced cheque that was later cleared has to show both.
/// </summary>
public enum ChequeOperationType
{
    Registered = 0,
    Deposited = 1,
    Issued = 2,
    Cleared = 3,
    Bounced = 4,
    Endorsed = 5,
    Returned = 6,
    Cancelled = 7,
    Extended = 8
}
