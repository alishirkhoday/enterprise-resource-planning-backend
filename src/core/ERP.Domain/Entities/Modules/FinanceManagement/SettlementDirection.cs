namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Whether money came in or went out.
/// </summary>
public enum SettlementDirection
{
    /// <summary>Money coming in.</summary>
    Receipt = 0,

    /// <summary>Money going out.</summary>
    Payment = 1
}
