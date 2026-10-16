namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Whether a cheque was taken in or written out. The two have completely different
/// lifecycles, which is why almost nothing about a cheque makes sense without it.
/// </summary>
public enum ChequeType
{
    /// <summary>A cheque handed to the organization by a customer or other payer.</summary>
    Received = 0,

    /// <summary>A cheque the organization wrote against its own bank account.</summary>
    Issued = 1
}
