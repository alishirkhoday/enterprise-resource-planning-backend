namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// How much of a debt owed to the business is left. Written off is deliberately not the same
/// as settled: one was paid, the other was given up on.
/// </summary>
public enum ReceivableStatus
{
    Open = 0,
    PartiallySettled = 1,
    Settled = 2,
    WrittenOff = 3
}
