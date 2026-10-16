namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Where a loan has got to.
/// </summary>
public enum LoanStatus
{
    Requested = 0,
    Approved = 1,
    Rejected = 2,

    /// <summary>Paid out and being repaid.</summary>
    Active = 3,

    Settled = 4,
    Cancelled = 5
}
