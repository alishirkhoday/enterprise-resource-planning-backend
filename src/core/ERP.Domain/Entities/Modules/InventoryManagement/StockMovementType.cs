namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Why stock moved. Every ledger entry carries one, and it is what lets a stock card be read
/// as a story rather than as a column of numbers.
/// </summary>
public enum StockMovementType
{
    /// <summary>The balance a period or an implementation starts with.</summary>
    OpeningBalance = 0,

    PurchaseReceipt = 1,
    PurchaseReturn = 2,
    SalesIssue = 3,
    SalesReturn = 4,

    /// <summary>Material consumed by a production order.</summary>
    ProductionIssue = 5,

    /// <summary>Finished goods delivered by a production order.</summary>
    ProductionReceipt = 6,

    TransferOut = 7,
    TransferIn = 8,

    /// <summary>A correction arising from a physical count.</summary>
    CountAdjustment = 9,

    /// <summary>Stock written off as damaged, expired or lost.</summary>
    WriteOff = 10,

    /// <summary>A change in value with no change in quantity.</summary>
    Revaluation = 11,

    /// <summary>
    /// A part consumed on a service visit. Kept apart from a sales issue because much of it goes
    /// out under warranty and never reaches an invoice, so the two would otherwise be impossible
    /// to tell apart in a margin report.
    /// </summary>
    ServiceIssue = 12,

    Other = 99
}
