namespace ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

/// <summary>
/// Where a warranty claim has got to. Assessment comes before any work is agreed to, which is
/// why raising a claim and accepting it are separate states.
/// </summary>
public enum WarrantyClaimStatus
{
    Registered = 0,
    UnderAssessment = 1,

    /// <summary>Accepted; the work to honour it may not be finished yet.</summary>
    Approved = 2,

    Rejected = 3,
    Closed = 4,
    Cancelled = 5
}
