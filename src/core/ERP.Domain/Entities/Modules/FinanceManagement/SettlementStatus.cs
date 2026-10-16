namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Whether a settlement has been confirmed. A draft has applied nothing.
/// </summary>
public enum SettlementStatus
{
    Draft = 0,
    Confirmed = 1,
    Cancelled = 2
}
