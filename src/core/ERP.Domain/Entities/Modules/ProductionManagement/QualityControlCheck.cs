using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// An inspection carried out against a quality plan, recording what was measured rather than
/// only whether it passed.
/// </summary>
public sealed class QualityControlCheck : BaseEntity
{
    public ProductionTask? Task { get; private set; }
    public Guid? TaskId { get; private set; }

    public Guid? ProductionOrderId { get; private set; }
    public Guid? InspectionPlanId { get; private set; }

    /// <summary>What was inspected, for example GoodsReceipt or ProductionReceipt.</summary>
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }

    public Human Inspector { get; private set; } = default!;
    public Guid InspectorId { get; private set; }

    public DateTimeOffset CheckedAt { get; private set; } = DateTimeOffset.UtcNow;

    public decimal InspectedQuantity { get; private set; }
    public decimal PassedQuantity { get; private set; }
    public decimal FailedQuantity => InspectedQuantity - PassedQuantity;

    public bool Passed { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<QualityMeasurement> _measurements = [];
    public IReadOnlyList<QualityMeasurement> Measurements => _measurements;

    private QualityControlCheck() { }

    public QualityControlCheck(Human inspector, decimal inspectedQuantity)
    {
        ArgumentNullException.ThrowIfNull(inspector, nameof(inspector));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inspectedQuantity, nameof(inspectedQuantity));
        Inspector = inspector;
        InspectorId = inspector.Id;
        InspectedQuantity = inspectedQuantity;
    }

    public void LinkToTask(ProductionTask task)
    {
        ArgumentNullException.ThrowIfNull(task, nameof(task));
        Task = task;
        TaskId = task.Id;
        ProductionOrderId = task.OrderId;
    }

    public void SetReference(string referenceType, string referenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
    }

    public void LinkToInspectionPlan(Guid inspectionPlanId) =>
        InspectionPlanId = inspectionPlanId == Guid.Empty ? null : inspectionPlanId;

    public void AddMeasurement(QualityMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement, nameof(measurement));
        _measurements.Add(measurement);
    }

    /// <summary>
    /// Records the verdict. The check fails if any quantity failed or any measurement was out of
    /// tolerance, so a pass cannot be recorded over a failing measurement.
    /// </summary>
    public void RecordResult(decimal passedQuantity, string? notes = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(passedQuantity, nameof(passedQuantity));
        if (passedQuantity > InspectedQuantity)
            throw new InvalidOperationException("More units cannot pass than were inspected.");

        PassedQuantity = passedQuantity;
        Passed = passedQuantity == InspectedQuantity && _measurements.All(m => m.IsWithinTolerance);
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        if (!Passed)
            Raise(new QualityCheckFailed(Id, ProductionOrderId, FailedQuantity));
    }

    public decimal PassRate => InspectedQuantity == 0m ? 0m : PassedQuantity / InspectedQuantity * 100m;
}
