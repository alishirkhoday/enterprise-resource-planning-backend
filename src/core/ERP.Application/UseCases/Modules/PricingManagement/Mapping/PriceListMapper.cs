using ERP.Application.UseCases.Modules.PricingManagement.DTOs;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.PricingManagement;
using ERP.Domain.Primitives.ValueObjects;

namespace ERP.Application.UseCases.Modules.PricingManagement.Mapping
{
    public static class PriceListMapper
    {
        public static PriceListDto ToDto(this PriceList priceList)
        {
            ArgumentNullException.ThrowIfNull(priceList, nameof(priceList));

            return new PriceListDto(
                priceList.Id,
                priceList.Code,
                priceList.Name,
                priceList.Type,
                priceList.CurrencyCode,
                priceList.ValidityPeriod.StartDate,
                priceList.ValidityPeriod.EndDate,
                priceList.IsActive,
                priceList.IsDefault,
                priceList.IsTaxInclusive,
                priceList.CustomerGroupId,
                priceList.Items.Count,
                [.. priceList.Items
                    .OrderBy(i => i.Item.Name)
                    .ThenBy(i => i.MinimumQuantity)
                    .Select(i => new PriceListItemDto(
                        i.Id,
                        i.ItemId,
                        i.Item.Name,
                        i.UnitOfMeasureId,
                        i.BasePrice.Amount,
                        i.MinimumQuantity,
                        i.DiscountPercentage?.Value,
                        i.NetPrice.Amount,
                        i.TaxPercentage?.Value,
                        i.PriceWithTax.Amount,
                        i.MinimumPrice?.Amount,
                        i.IsActive))]);
        }

        /// <summary>
        /// Builds one priced line. Shared by creating a list and adding to one, which differ only
        /// in whether the list already existed.
        /// </summary>
        public static PriceListItem ToEntity(this PriceListLineDto dto, PriceList priceList, Item item)
        {
            ArgumentNullException.ThrowIfNull(dto, nameof(dto));

            return new PriceListItem(
                priceList, item, dto.UnitOfMeasureId,
                Money.Create(dto.BasePrice, priceList.CurrencyCode), dto.MinimumQuantity,
                dto.DiscountPercentage is null ? null : Percentage.Create(dto.DiscountPercentage.Value),
                dto.TaxId,
                dto.TaxPercentage is null ? null : Percentage.Create(dto.TaxPercentage.Value),
                dto.MinimumPrice is null ? null : Money.Create(dto.MinimumPrice.Value, priceList.CurrencyCode));
        }
    }
}
