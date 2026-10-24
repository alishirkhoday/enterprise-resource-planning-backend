namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Something an inspection measures, and the range it must fall inside to be acceptable.
/// </summary>
public sealed class QualityParameter : AggregateRoot
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public QualityParameterType Type { get; private set; }

    public string? UnitOfMeasure { get; private set; }

    /// <summary>The value aimed for, for a numeric parameter.</summary>
    public decimal? TargetValue { get; private set; }
    public decimal? MinimumValue { get; private set; }
    public decimal? MaximumValue { get; private set; }

    /// <summary>Whether a failure here fails the whole inspection outright.</summary>
    public bool IsCritical { get; private set; }

    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private QualityParameter() { }

    public QualityParameter(string code, string name, QualityParameterType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Code = code.Trim();
        Name = name.Trim();
        Type = type;
        IsActive = true;
    }

    public void SetTolerance(decimal? targetValue, decimal? minimumValue, decimal? maximumValue, string? unitOfMeasure)
    {
        if (Type != QualityParameterType.Numeric)
            throw new InvalidOperationException("A tolerance only applies to a numeric parameter.");
        if (minimumValue.HasValue && maximumValue.HasValue && maximumValue.Value < minimumValue.Value)
            throw new ArgumentOutOfRangeException(nameof(maximumValue), "The maximum cannot be below the minimum.");

        TargetValue = targetValue;
        MinimumValue = minimumValue;
        MaximumValue = maximumValue;
        UnitOfMeasure = string.IsNullOrWhiteSpace(unitOfMeasure) ? null : unitOfMeasure.Trim();
    }

    public void MarkAsCritical() => IsCritical = true;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public bool IsWithinTolerance(decimal value)
    {
        if (MinimumValue.HasValue && value < MinimumValue.Value)
            return false;
        if (MaximumValue.HasValue && value > MaximumValue.Value)
            return false;
        return true;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
