using ERP.Application.UseCases.Modules.PricingManagement.DTOs;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.PricingManagement;

namespace ERP.Application.UseCases.Modules.PricingManagement.Queries.GetEffectivePrice
{
    /// <summary>
    /// What one item costs a given customer today - استعلام قیمت.
    /// <para>
    /// It goes through the same resolution an invoice will: the customer's own list beats their
    /// group's, which beats the default, and the quantity picks the volume break. Answering this
    /// question any other way would let a salesperson quote a figure the invoice then contradicts.
    /// </para>
    /// </summary>
    public class GetEffectivePriceQueryHandler(
        IItemRepository items,
        ICustomerRepository customers,
        IPriceListRepository priceLists,
        TimeProvider timeProvider)
        : IRequestHandler<GetEffectivePriceQuery, Result<EffectivePriceDto, Error>>
    {
        public async Task<Result<EffectivePriceDto, Error>> Handle(GetEffectivePriceQuery request, CancellationToken cancellationToken)
        {
            var item = await items.GetByIdAsync(request.ItemId, cancellationToken);
            if (item is null)
                return Result.Failure<EffectivePriceDto, Error>(Errors.General.NotFound(request.ItemId.ToString(), nameof(Item)));

            var onDate = request.OnDate ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

            Customer? customer = null;

            if (request.CustomerId is not null)
            {
                customer = await customers.GetByIdAsync(request.CustomerId.Value, cancellationToken);
                if (customer is null)
                    return Result.Failure<EffectivePriceDto, Error>(Errors.General.NotFound(request.CustomerId.Value.ToString(), nameof(Customer)));
            }

            var priceList = await priceLists.GetEffectiveForSaleAsync(
                request.PriceListId ?? customer?.PriceListId,
                customer?.CustomerGroupId,
                request.CurrencyCode?.Trim().ToUpperInvariant() ?? "IRR",
                onDate,
                cancellationToken);

            var priced = priceList?.PriceFor(item.Id, request.Quantity);
            if (priceList is null || priced is null)
                return Result.Failure<EffectivePriceDto, Error>(
                    Errors.Pricing.NoPriceForItem(item.Name ?? item.Id.ToString(), onDate));

            return Result.Success<EffectivePriceDto, Error>(new EffectivePriceDto(
                item.Id,
                item.Name,
                request.Quantity,
                priceList.Id,
                priceList.Code,
                priceList.CurrencyCode,
                priced.BasePrice.Amount,
                priced.DiscountPercentage?.Value,
                priced.NetPrice.Amount,
                priced.TaxAmount.Amount,
                priced.PriceWithTax.Amount,
                // The line total is what the salesperson is actually about to quote, which is the
                // number they would otherwise work out on a calculator and get wrong.
                priced.PriceWithTax.Multiply(request.Quantity).Amount,
                priced.MinimumPrice?.Amount));
        }
    }
}
