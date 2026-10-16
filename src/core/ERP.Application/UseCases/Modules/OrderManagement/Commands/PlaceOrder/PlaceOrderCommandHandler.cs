using ERP.Application.UseCases.Modules.OrderManagement.DTOs;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.OrderManagement;
using ERP.Domain.Entities.Modules.OrderManagement.ValueObjects;
using ERP.Domain.Entities.Modules.PricingManagement;
using ERP.Domain.Primitives.ValueObjects;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.OrderManagement;
using ERP.Domain.Repositories.Modules.PricingManagement;

namespace ERP.Application.UseCases.Modules.OrderManagement.Commands.PlaceOrder
{
    /// <summary>
    /// Turns a basket into an order and starts the payment clock.
    /// <para>
    /// Everything is checked before the order is built. Half an order that then discovers an item
    /// has no price is worse than no order, and the unit of work would discard it anyway - but the
    /// customer would already have been told something different.
    /// </para>
    /// </summary>
    public class PlaceOrderCommandHandler(
        ICustomerRepository customers,
        IItemRepository items,
        IPriceListRepository priceLists,
        IOrderRepository orders,
        INumberingSeriesRepository numberingSeries,
        TimeProvider timeProvider) : IRequestHandler<PlaceOrderCommand, Result<PlaceOrderResultDto, Error>>
    {
        /// <summary>The numbering series an order's code is drawn from.</summary>
        private const string OrderNumberingKey = "Order";

        public async Task<Result<PlaceOrderResultDto, Error>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
        {
            var currencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

            var customer = await customers.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer is null)
                return Result.Failure<PlaceOrderResultDto, Error>(Errors.General.NotFound(request.CustomerId.ToString(), nameof(Customer)));

            if (!customer.CanBeSoldTo)
                return Result.Failure<PlaceOrderResultDto, Error>(Errors.Order.CustomerCannotBeSoldTo(customer.DisplayName));

            var priceList = await priceLists.GetEffectiveForSaleAsync(
                customer.PriceListId,
                customer.CustomerGroupId,
                currencyCode,
                today,
                cancellationToken);

            if (priceList is null)
                return Result.Failure<PlaceOrderResultDto, Error>(Errors.Order.NoEffectivePriceList(currencyCode));

            var lines = await ResolveLinesAsync(request, priceList, cancellationToken);
            if (lines.IsFailure)
                return Result.Failure<PlaceOrderResultDto, Error>(lines.Error);

            var series = await numberingSeries.GetForUpdateAsync(OrderNumberingKey, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<PlaceOrderResultDto, Error>(Errors.Order.NumberingSeriesMissing(OrderNumberingKey));

            var order = new Order(customer, series.Next(today), currencyCode);

            short lineNumber = 1;
            foreach (var line in lines.Value)
            {
                order.AddItem(new OrderItem(order, line.Item, lineNumber++, line.UnitPrice, line.Quantity, line.UnitOfMeasureId));
            }

            order.SetAddress(BuildShipToAddress(request.ShipTo));
            order.SetTimeLeftToPayment(request.PaymentWindowMinutes);

            if (request.WarehouseId is not null)
                order.SetFulfillmentSource(request.WarehouseId.Value);

            // The last step, and the one that records the fact. Everything above only assembled it.
            order.Place();

            await orders.CreateAsync(order, cancellationToken);

            return Result.Success<PlaceOrderResultDto, Error>(new PlaceOrderResultDto(
                order.Id,
                order.Code,
                order.AmountPayable.Amount,
                order.CurrencyCode,
                order.TimeLeftToPayment));
        }

        /// <summary>
        /// Loads each item and prices it from the list. Every line is resolved before any of them
        /// is added, so an order is never partly built around an item that turns out to be
        /// unsellable or unpriced.
        /// </summary>
        private async Task<Result<List<ResolvedLine>, Error>> ResolveLinesAsync(
            PlaceOrderCommand request,
            PriceList priceList,
            CancellationToken cancellationToken)
        {
            var resolved = new List<ResolvedLine>(request.Items.Count);

            var found = await items.GetByIdsAsync([.. request.Items.Select(i => i.ItemId)], cancellationToken);
            var byId = found.ToDictionary(i => i.Id);

            foreach (var requested in request.Items)
            {
                if (!byId.TryGetValue(requested.ItemId, out var item))
                    return Result.Failure<List<ResolvedLine>, Error>(Errors.General.NotFound(requested.ItemId.ToString(), nameof(Item)));

                if (!item.IsSellable)
                    return Result.Failure<List<ResolvedLine>, Error>(Errors.Order.ItemIsNotSellable(item.Name ?? item.StockKeepingUnit ?? requested.ItemId.ToString()));

                var priced = priceList.PriceFor(item.Id, requested.Quantity);
                if (priced is null)
                    return Result.Failure<List<ResolvedLine>, Error>(Errors.Order.ItemHasNoPrice(item.Name ?? requested.ItemId.ToString(), priceList.Code));

                resolved.Add(new ResolvedLine(item, requested.Quantity, priced.NetPrice, priced.UnitOfMeasureId));
            }

            return Result.Success<List<ResolvedLine>, Error>(resolved);
        }

        private static ShipToAddress BuildShipToAddress(PlaceOrderAddressDto dto)
        {
            var address = new Address(dto.Country, dto.Province, dto.City, dto.Region, dto.Street, dto.Plaque, dto.PostalCode);
            address.SetGeographyReferences(dto.CountryId, dto.ProvinceId, dto.CityId);
            address.SetUnit(dto.Unit);

            var shipTo = new ShipToAddress(address, dto.ReceiverFullName, dto.ReceiverMobileNumber);
            shipTo.SetDeliveryInstruction(dto.DeliveryInstruction);

            return shipTo;
        }

        private sealed record ResolvedLine(Item Item, decimal Quantity, Money UnitPrice, Guid UnitOfMeasureId);
    }
}
