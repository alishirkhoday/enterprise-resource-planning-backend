namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// How much of the warehouse a count covers - کامل، دوره‌ای یا موردی.
/// </summary>
public enum StockCountType
{
    /// <summary>Everything in the warehouse, normally at year end.</summary>
    Full = 0,

    /// <summary>A rolling count of a slice of the stock, so the whole is covered over time.</summary>
    Cycle = 1,

    /// <summary>A one off check of a particular item or location.</summary>
    Spot = 2
}
