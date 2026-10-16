namespace ERP.Application.UseCases.Modules.ProductionManagement.DTOs
{
    /// <summary>
    /// An inspection - کنترل کیفیت. What was measured is recorded, not only whether it passed:
    /// a verdict tells you this batch was wrong, readings tell you the process is drifting.
    /// </summary>
    public record RecordQualityCheckDto
    {
        public Guid InspectorHumanId { get; init; }

        public decimal InspectedQuantity { get; init; }

        public decimal PassedQuantity { get; init; }

        public IReadOnlyList<QualityMeasurementDto> Measurements { get; init; } = [];

        public Guid? ProductionTaskId { get; init; }

        public Guid? InspectionPlanId { get; init; }

        /// <summary>What was inspected, such as a GoodsReceipt or a ProductionReceipt.</summary>
        public string? ReferenceType { get; init; }

        public string? ReferenceId { get; init; }

        public string? Notes { get; init; }
    }

    public record QualityMeasurementDto
    {
        public Guid ParameterId { get; init; }

        /// <summary>The reading, for a parameter measured numerically.</summary>
        public decimal? NumericValue { get; init; }

        /// <summary>The observation, for a parameter judged by eye.</summary>
        public string? TextValue { get; init; }

        /// <summary>
        /// Whether the observation is acceptable. Only consulted for a judged parameter; a
        /// numeric one is checked against its own tolerance and cannot be talked into passing.
        /// </summary>
        public bool IsAcceptable { get; init; }

        public string? Notes { get; init; }
    }

    public sealed record QualityCheckDto(
        Guid Id,
        Guid InspectorId,
        string InspectorName,
        DateTimeOffset CheckedAt,
        Guid? ProductionOrderId,
        Guid? TaskId,
        string? ReferenceType,
        string? ReferenceId,
        decimal InspectedQuantity,
        decimal PassedQuantity,
        decimal FailedQuantity,
        decimal PassRate,
        bool Passed,
        string? Notes,
        IReadOnlyList<QualityMeasurementViewDto> Measurements);

    public sealed record QualityMeasurementViewDto(
        Guid Id,
        Guid ParameterId,
        string ParameterName,
        bool IsCritical,
        decimal? NumericValue,
        string? TextValue,
        decimal? TargetValue,
        decimal? MinimumValue,
        decimal? MaximumValue,
        bool IsWithinTolerance,
        string? Notes);
}
