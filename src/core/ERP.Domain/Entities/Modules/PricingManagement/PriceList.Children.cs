using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.PricingManagement;

public sealed partial class PriceList
{
    public void SetPriceListItemBasePrice(Guid childId, Money basePrice)
    {
        var child = GetChild(childId, _items);
        child.SetBasePrice(basePrice);
    }

    public void SetPriceListItemDiscount(Guid childId, Percentage? discountPercentage)
    {
        var child = GetChild(childId, _items);
        child.SetDiscount(discountPercentage);
    }

    public void SetPriceListItemTax(Guid childId, Guid? taxId, Percentage? taxPercentage)
    {
        var child = GetChild(childId, _items);
        child.SetTax(taxId, taxPercentage);
    }

    public void SetPriceListItemMinimumPrice(Guid childId, Money minimumPrice)
    {
        var child = GetChild(childId, _items);
        child.SetMinimumPrice(minimumPrice);
    }

    public void ActivatePriceListItem(Guid childId)
    {
        var child = GetChild(childId, _items);
        child.Activate();
    }

    public void DeactivatePriceListItem(Guid childId)
    {
        var child = GetChild(childId, _items);
        child.Deactivate();
    }
}
