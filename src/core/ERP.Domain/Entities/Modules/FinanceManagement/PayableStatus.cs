namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// How much of a debt the business owes is left.
/// </summary>
public enum PayableStatus
{
    Open = 0,
    PartiallySettled = 1,
    Settled = 2,
    Cancelled = 3
}
