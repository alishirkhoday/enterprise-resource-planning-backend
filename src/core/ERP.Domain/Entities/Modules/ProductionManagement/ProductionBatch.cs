using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// A run of output that shares a batch number, so finished goods can be traced back to the
/// materials and the shift that made them.
/// </summary>
public sealed class ProductionBatch : BaseEntity
{
    public string BatchCode { get; private set; } = default!;
    public Item Item { get; private set; } = default!;
    public int Quantity { get; private set; }
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset? EndDate { get; private set; }
    public string? Notes { get; private set; }

    // Required by Entity Framework to materialise the entity.
    private ProductionBatch() { }

    public ProductionBatch(string batchCode, Item item, int quantity, DateTimeOffset startDate, string? notes = null)
    {
        BatchCode = batchCode;
        Item = item;
        Quantity = quantity;
        StartDate = startDate;
        Notes = notes;
    }

    public void EndBatch(DateTimeOffset endDate)
    {
        EndDate = endDate;
    }
}
