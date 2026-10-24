namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Defects found in a run - گزارش ضایعات. What a process improvement is argued from.
/// </summary>
public sealed class ProductionDefectReport : AggregateRoot
{
    public ProductionTask Task { get; private set; } = default!;
    public string DefectType { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public int Quantity { get; private set; }
    public DateTimeOffset ReportedAt { get; private set; } = DateTimeOffset.UtcNow;

    // Required by Entity Framework to materialise the entity.
    private ProductionDefectReport() { }

    public ProductionDefectReport(ProductionTask task, string defectType, string description, int quantity)
    {
        Task = task;
        DefectType = defectType;
        Description = description;
        Quantity = quantity;
    }
}
