namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// One reading taken during an inspection, against the tolerance the parameter allows.
/// </summary>
public sealed class QualityMeasurement : BaseEntity
{
    public QualityControlCheck Check { get; private set; } = default!;
    public Guid CheckId { get; private set; }

    public QualityParameter Parameter { get; private set; } = default!;
    public Guid ParameterId { get; private set; }

    /// <summary>The reading, for a parameter measured numerically.</summary>
    public decimal? NumericValue { get; private set; }

    /// <summary>The observation, for a parameter judged by eye.</summary>
    public string? TextValue { get; private set; }

    public bool IsWithinTolerance { get; private set; }
    public string? Notes { get; private set; }

    private QualityMeasurement() { }

    public QualityMeasurement(QualityControlCheck check, QualityParameter parameter, decimal numericValue)
    {
        ArgumentNullException.ThrowIfNull(check, nameof(check));
        ArgumentNullException.ThrowIfNull(parameter, nameof(parameter));
        Check = check;
        CheckId = check.Id;
        Parameter = parameter;
        ParameterId = parameter.Id;
        NumericValue = numericValue;
        IsWithinTolerance = parameter.IsWithinTolerance(numericValue);
    }

    public QualityMeasurement(QualityControlCheck check, QualityParameter parameter, string textValue, bool isAcceptable)
    {
        ArgumentNullException.ThrowIfNull(check, nameof(check));
        ArgumentNullException.ThrowIfNull(parameter, nameof(parameter));
        ArgumentException.ThrowIfNullOrWhiteSpace(textValue, nameof(textValue));
        Check = check;
        CheckId = check.Id;
        Parameter = parameter;
        ParameterId = parameter.Id;
        TextValue = textValue.Trim();
        IsWithinTolerance = isAcceptable;
    }

    public void SetNotes(string? notes) => Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}
