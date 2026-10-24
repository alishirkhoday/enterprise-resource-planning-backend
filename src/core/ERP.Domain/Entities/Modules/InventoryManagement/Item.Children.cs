namespace ERP.Domain.Entities.Modules.InventoryManagement;

public sealed partial class Item
{
    public void SetItemBarcodeUnitOfMeasure(Guid childId, Guid unitOfMeasureId)
    {
        var child = GetChild(childId, _barcodes);
        child.SetUnitOfMeasure(unitOfMeasureId);
    }

    public void MarkItemBarcodeAsPrimary(Guid childId)
    {
        var child = GetChild(childId, _barcodes);
        foreach (var sibling in _barcodes.Where(sibling => sibling.Id != childId))
            sibling.UnmarkAsPrimary();
        child.MarkAsPrimary();
    }

    public void UnmarkItemBarcodeAsPrimary(Guid childId)
    {
        var child = GetChild(childId, _barcodes);
        child.UnmarkAsPrimary();
    }

    public void SetItemBarcodeDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _barcodes);
        child.SetDescription(description);
    }

    public void SetItemSupplierSupplierItemCode(Guid childId, string? supplierItemCode)
    {
        var child = GetChild(childId, _suppliers);
        child.SetSupplierItemCode(supplierItemCode);
    }

    public void RecordItemSupplierPurchase(Guid childId, Money price, DateOnly purchaseDate)
    {
        var child = GetChild(childId, _suppliers);
        child.RecordPurchase(price, purchaseDate);
    }

    public void SetItemSupplierOrderingRules(Guid childId, decimal? minimumOrderQuantity, decimal? orderMultiple, int? leadTimeDays)
    {
        var child = GetChild(childId, _suppliers);
        child.SetOrderingRules(minimumOrderQuantity, orderMultiple, leadTimeDays);
    }

    public void SetItemSupplierDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _suppliers);
        child.SetDescription(description);
    }

    public void SetItemSupplierPreferred(Guid childId)
    {
        var child = GetChild(childId, _suppliers);
        foreach (var sibling in _suppliers.Where(sibling => sibling.Id != childId))
            sibling.UnsetPreferred();
        child.SetPreferred();
    }

    public void UnsetPreferredItemSupplier(Guid childId)
    {
        var child = GetChild(childId, _suppliers);
        child.UnsetPreferred();
    }

    public void ActivateItemSupplier(Guid childId)
    {
        var child = GetChild(childId, _suppliers);
        child.Activate();
    }

    public void DeactivateItemSupplier(Guid childId)
    {
        var child = GetChild(childId, _suppliers);
        child.Deactivate();
    }
}
