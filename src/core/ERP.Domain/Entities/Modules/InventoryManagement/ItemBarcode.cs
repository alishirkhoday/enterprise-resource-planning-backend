namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// One scannable code for an item. An item normally carries several, because a carton and a
/// single unit scan differently.
/// </summary>
public sealed class ItemBarcode : BaseEntity
{
    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    public string Value { get; private set; } = default!;
    public BarcodeSymbology Symbology { get; private set; }

    /// <summary>The unit this code identifies, so scanning a carton adds the right quantity.</summary>
    public Guid? UnitOfMeasureId { get; private set; }

    public bool IsPrimary { get; private set; }
    public string? Description { get; private set; }

    private ItemBarcode() { }

    public ItemBarcode(Item item, string value, BarcodeSymbology symbology = BarcodeSymbology.Ean13)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        Item = item;
        ItemId = item.Id;
        Value = value.Trim();
        Symbology = symbology;
    }

    public void SetUnitOfMeasure(Guid unitOfMeasureId) => UnitOfMeasureId = unitOfMeasureId == Guid.Empty ? null : unitOfMeasureId;

    public void MarkAsPrimary() => IsPrimary = true;
    public void UnmarkAsPrimary() => IsPrimary = false;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
