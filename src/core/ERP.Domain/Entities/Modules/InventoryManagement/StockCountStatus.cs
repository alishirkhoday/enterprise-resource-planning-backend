namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Where a count sheet has got to. Freezing is what fixes the book quantities the count is
/// measured against, so nothing before it is comparable with anything after.
/// </summary>
public enum StockCountStatus
{
    Planned = 0,

    /// <summary>Book quantities are frozen and counting is under way.</summary>
    InProgress = 1,

    AwaitingApproval = 2,
    Completed = 3,
    Cancelled = 4
}
