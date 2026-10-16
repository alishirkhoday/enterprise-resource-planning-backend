namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// What the shop floor reported for a period - output, scrap, downtime.
/// </summary>
public sealed class ProductionReport : BaseEntity
{
    public ProductionOrder Order { get; private set; } = default!;
    public int ActualQuantityProduced { get; private set; }
    public int ScrapQuantity { get; private set; }
    public string? Notes { get; private set; }

    // Required by Entity Framework to materialise the entity.
    private ProductionReport() { }

    public ProductionReport(ProductionOrder order, int actualQuantityProduced, int scrapQuantity, string? notes)
    {
        Order = order;
        ActualQuantityProduced = actualQuantityProduced;
        ScrapQuantity = scrapQuantity;
        Notes = notes;
    }
}
