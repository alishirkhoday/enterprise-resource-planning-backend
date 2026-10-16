namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Why an asset left the books, which decides how the gain or loss on it is treated.
/// </summary>
public enum AssetDisposalReason
{
    Sold = 0,
    Scrapped = 1,
    Donated = 2,
    Stolen = 3,
    Destroyed = 4,
    TradedIn = 5,
    Other = 99
}
