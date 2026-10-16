namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Where a financial year has got to. Only an open year accepts new documents.
/// </summary>
public enum FinancialYearStatus
{
    /// <summary>Being prepared; the opening entry has not been posted yet.</summary>
    Draft = 0,

    Open = 1,

    /// <summary>Closing entries are being prepared, so only adjustments are accepted.</summary>
    Closing = 2,

    Closed = 3
}
