namespace ERP.Domain.Entities.Modules.WarehouseManagement;

/// <summary>
/// Where a transfer between warehouses has got to. In transit is the state that matters: that
/// stock belongs to neither end and cannot be found by walking around either of them.
/// </summary>
public enum StockTransferStatus
{
    Draft = 0,
    Approved = 1,

    /// <summary>Gone from the source and not yet at the destination.</summary>
    InTransit = 2,

    Received = 3,

    /// <summary>Arrived, but less than was dispatched.</summary>
    ReceivedWithDiscrepancy = 4,

    Cancelled = 5
}
