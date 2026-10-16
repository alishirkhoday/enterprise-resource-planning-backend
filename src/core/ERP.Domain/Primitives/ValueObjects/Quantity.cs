namespace ERP.Domain.Primitives.ValueObjects;

/// <summary>
/// An amount of something expressed in a specific unit of measure. Arithmetic between different
/// units is rejected; convert through a UnitOfMeasureConversion first.
/// </summary>
public sealed class Quantity : ValueObject
{
    public decimal Value { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }

    private Quantity() { }

    private Quantity(decimal value, Guid unitOfMeasureId)
    {
        if (unitOfMeasureId == Guid.Empty)
            throw new ArgumentException("UnitOfMeasureId is empty.", nameof(unitOfMeasureId));
        Value = value;
        UnitOfMeasureId = unitOfMeasureId;
    }

    public static Quantity Create(decimal value, Guid unitOfMeasureId) => new(value, unitOfMeasureId);
    public static Quantity Zero(Guid unitOfMeasureId) => new(0m, unitOfMeasureId);

    public bool IsZero => Value == 0m;
    public bool IsPositive => Value > 0m;

    public Quantity Add(Quantity other)
    {
        EnsureSameUnit(other);
        return new Quantity(Value + other.Value, UnitOfMeasureId);
    }

    public Quantity Subtract(Quantity other)
    {
        EnsureSameUnit(other);
        return new Quantity(Value - other.Value, UnitOfMeasureId);
    }

    public Quantity Multiply(decimal factor) => new(Value * factor, UnitOfMeasureId);

    public Quantity ConvertTo(Guid targetUnitOfMeasureId, decimal factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor, nameof(factor));
        return new Quantity(Value * factor, targetUnitOfMeasureId);
    }

    private void EnsureSameUnit(Quantity other)
    {
        ArgumentNullException.ThrowIfNull(other, nameof(other));
        if (UnitOfMeasureId != other.UnitOfMeasureId)
            throw new InvalidOperationException("Unit of measure mismatch.");
    }

    public static Quantity operator +(Quantity left, Quantity right) => left.Add(right);
    public static Quantity operator -(Quantity left, Quantity right) => left.Subtract(right);
    public static Quantity operator *(Quantity left, decimal factor) => left.Multiply(factor);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return UnitOfMeasureId;
    }

    public override string ToString() => Value.ToString();
}
