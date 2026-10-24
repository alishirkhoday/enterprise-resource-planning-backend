namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// How to get from one unit to another. Without a conversion, a quantity in boxes and one in
/// pieces cannot be added, which is exactly what the Quantity value object refuses to do.
/// </summary>
public sealed class UnitOfMeasureConversion : AggregateRoot
{
    public Guid FromUnit { get; private set; } = default!;
    public Guid ToUnit { get; private set; } = default!;
    public decimal Factor { get; private set; }
    public string? Description { get; private set; }

    private UnitOfMeasureConversion()
    {
    }

    public UnitOfMeasureConversion(Guid fromUnit, Guid toUnit, decimal factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor, nameof(factor));
        FromUnit = fromUnit;
        ToUnit = toUnit;
        Factor = factor;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrEmpty(description) ? null : description.Trim();
}
