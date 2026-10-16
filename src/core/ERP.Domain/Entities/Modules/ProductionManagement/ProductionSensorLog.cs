namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Readings taken off a machine automatically. High volume and append-only, which is why it
/// is kept apart from the reports people write.
/// </summary>
public sealed class ProductionSensorLog : BaseEntity
{
    public Machine Machine { get; private set; } = default!;
    public string SensorName { get; private set; } = default!;
    public string Metric { get; private set; } = default!;
    public decimal Value { get; private set; }
    public DateTimeOffset CapturedAt { get; private set; } = DateTimeOffset.UtcNow;

    // Required by Entity Framework to materialise the entity.
    private ProductionSensorLog() { }

    public ProductionSensorLog(Machine machine, string sensorName, string metric, decimal value)
    {
        Machine = machine;
        SensorName = sensorName;
        Metric = metric;
        Value = value;
    }
}
