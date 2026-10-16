namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Whether a period still accepts postings. A closed period takes nothing, which is the whole
/// reason the status exists.
/// </summary>
public enum AccountingPeriodStatus
{
    Open = 0,

    /// <summary>Temporarily refuses postings while the period is under review.</summary>
    Locked = 1,

    Closed = 2
}
