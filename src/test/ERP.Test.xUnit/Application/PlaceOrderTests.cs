using CSharpFunctionalExtensions;
using ERP.Application.Common;
using ERP.Application.UseCases.Modules.OrderManagement.Commands.PlaceOrder;
using ERP.Application.UseCases.Modules.OrderManagement.DTOs;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.PricingManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.OrderManagement;
using ERP.Domain.Repositories.Modules.PricingManagement;
using Moq;
using Order = ERP.Domain.Entities.Modules.OrderManagement.Order;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// The first slice that goes all the way through: a request becomes an order, priced from the
    /// list rather than from the caller, and the fact that it was placed is recorded.
    /// </summary>
    public sealed class PlaceOrderTests
    {
        private static readonly Guid Unit = Guid.NewGuid();
        private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private static Item CreateItem(string name = "Pump")
        {
            var item = new Item(new ItemGroup("G-1", "Pumps"), ItemType.Product);
            item.Update("SKU-1", name);
            return item;
        }

        private static PriceList CreatePriceList(Item item, decimal price = 1_000_000m, decimal minimumQuantity = 1m)
        {
            var list = new PriceList(
                "PL-1",
                "Retail",
                PriceListType.Sales,
                "IRR",
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));

            list.AddItem(new PriceListItem(list, item, Unit, Money.Create(price, "IRR"), minimumQuantity));
            return list;
        }

        private static PlaceOrderCommand CreateCommand(Guid customerId, params (Guid ItemId, decimal Quantity)[] lines) => new()
        {
            CustomerId = customerId,
            CurrencyCode = "IRR",
            Items = [.. lines.Select(l => new PlaceOrderItemDto { ItemId = l.ItemId, Quantity = l.Quantity })],
            PaymentWindowMinutes = 30,
            ShipTo = new PlaceOrderAddressDto
            {
                ReceiverFullName = "Kaveh Ahangar",
                ReceiverMobileNumber = "09123456789",
                Country = "Iran",
                Province = "Tehran",
                City = "Tehran",
                Region = "District 3",
                Street = "Mirdamad",
                Plaque = "12",
                PostalCode = "1234567890"
            }
        };

        private sealed class Harness
        {
            public Mock<ICustomerRepository> Customers { get; } = new();
            public Mock<IItemRepository> Items { get; } = new();
            public Mock<IPriceListRepository> PriceLists { get; } = new();
            public Mock<IOrderRepository> Orders { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public Order? Created { get; private set; }

            public Harness(Customer? customer, IEnumerable<Item> items, PriceList? priceList, NumberingSeries? series)
            {
                Customers.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(customer);

                Items.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync([.. items]);

                PriceLists.Setup(r => r.GetEffectiveForSaleAsync(
                        It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(priceList);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(series);

                Orders.Setup(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                    .Callback<Order, CancellationToken>((order, _) => Created = order)
                    .Returns(Task.CompletedTask);
            }

            public PlaceOrderCommandHandler Build() => new(
                Customers.Object,
                Items.Object,
                PriceLists.Object,
                Orders.Object,
                Series.Object,
                new FakeTimeProvider(Now));
        }

        private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private static NumberingSeries CreateSeries() => new("Order", paddingLength: 5);

        [Fact]
        public async Task An_order_is_priced_from_the_list_not_from_the_caller()
        {
            var item = CreateItem();
            var customer = new Customer("C-1", "Sepehr Trading", CustomerType.Organization);
            var harness = new Harness(customer, [item], CreatePriceList(item, 1_000_000m), CreateSeries());

            var result = await harness.Build().Handle(CreateCommand(customer.Id, (item.Id, 3m)), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(3_000_000m, result.Value.AmountPayable);
            Assert.Equal("IRR", result.Value.CurrencyCode);
        }

        /// <summary>
        /// The reason the slice exists: placing an order records a fact, which the outbox then
        /// carries to whoever reserves the stock and tells the customer.
        /// </summary>
        [Fact]
        public async Task Placing_an_order_records_that_it_was_placed()
        {
            var item = CreateItem();
            var customer = new Customer("C-1", "Sepehr Trading", CustomerType.Organization);
            var harness = new Harness(customer, [item], CreatePriceList(item), CreateSeries());

            await harness.Build().Handle(CreateCommand(customer.Id, (item.Id, 1m)), Token);

            var placed = Assert.Single(harness.Created!.DomainEvents.OfType<OrderPlaced>());
            Assert.Equal(customer.Id, placed.CustomerId);
            Assert.Equal(harness.Created.Code, placed.Code);
        }

        [Fact]
        public async Task The_payment_window_starts_when_the_order_is_placed()
        {
            var item = CreateItem();
            var customer = new Customer("C-1", "Sepehr Trading", CustomerType.Organization);
            var harness = new Harness(customer, [item], CreatePriceList(item), CreateSeries());

            var result = await harness.Build().Handle(CreateCommand(customer.Id, (item.Id, 1m)), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(harness.Created!.RegistrationDateTime.AddMinutes(30), result.Value.PayBefore);
        }

        [Fact]
        public async Task An_order_takes_its_code_from_the_numbering_series()
        {
            var item = CreateItem();
            var customer = new Customer("C-1", "Sepehr Trading", CustomerType.Organization);
            var series = new NumberingSeries("Order", prefix: "ORD-", paddingLength: 5);
            var harness = new Harness(customer, [item], CreatePriceList(item), series);

            var result = await harness.Build().Handle(CreateCommand(customer.Id, (item.Id, 1m)), Token);

            Assert.StartsWith("ORD-", result.Value.Code, StringComparison.Ordinal);
        }

        /// <summary>
        /// Volume breaks come from the list, so asking for more can legitimately cost less per unit.
        /// </summary>
        [Fact]
        public async Task A_volume_break_applies_when_the_quantity_qualifies()
        {
            var item = CreateItem();
            var list = CreatePriceList(item, 1_000_000m);
            list.AddItem(new PriceListItem(list, item, Unit, Money.Create(800_000m, "IRR"), minimumQuantity: 10m));

            var customer = new Customer("C-1", "Sepehr Trading", CustomerType.Organization);
            var harness = new Harness(customer, [item], list, CreateSeries());

            var result = await harness.Build().Handle(CreateCommand(customer.Id, (item.Id, 10m)), Token);

            Assert.Equal(8_000_000m, result.Value.AmountPayable);
        }

        [Fact]
        public async Task An_unknown_customer_is_refused()
        {
            var item = CreateItem();
            var harness = new Harness(null, [item], CreatePriceList(item), CreateSeries());

            var result = await harness.Build().Handle(CreateCommand(Guid.NewGuid(), (item.Id, 1m)), Token);

            Assert.True(result.IsFailure);
            Assert.Equal(Errors.General.NotFound(), result.Error);
        }

        [Fact]
        public async Task A_customer_who_cannot_be_sold_to_is_refused()
        {
            var item = CreateItem();
            var customer = new Customer("C-1", "Sepehr Trading", CustomerType.Organization);
            customer.Suspend("Unpaid invoices.");
            var harness = new Harness(customer, [item], CreatePriceList(item), CreateSeries());

            var result = await harness.Build().Handle(CreateCommand(customer.Id, (item.Id, 1m)), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("CustomerCannotBeSoldTo", result.Error.Code);
        }

        [Fact]
        public async Task An_item_with_no_price_is_refused()
        {
            var priced = CreateItem();
            var unpriced = CreateItem("Valve");
            var customer = new Customer("C-1", "Sepehr Trading", CustomerType.Organization);
            var harness = new Harness(customer, [priced, unpriced], CreatePriceList(priced), CreateSeries());

            var result = await harness.Build().Handle(CreateCommand(customer.Id, (unpriced.Id, 1m)), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("ItemHasNoPrice", result.Error.Code);
        }

        [Fact]
        public async Task An_order_with_no_effective_price_list_is_refused()
        {
            var item = CreateItem();
            var customer = new Customer("C-1", "Sepehr Trading", CustomerType.Organization);
            var harness = new Harness(customer, [item], null, CreateSeries());

            var result = await harness.Build().Handle(CreateCommand(customer.Id, (item.Id, 1m)), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("NoEffectivePriceList", result.Error.Code);
        }

        [Fact]
        public async Task A_missing_numbering_series_is_refused_rather_than_invented()
        {
            var item = CreateItem();
            var customer = new Customer("C-1", "Sepehr Trading", CustomerType.Organization);
            var harness = new Harness(customer, [item], CreatePriceList(item), null);

            var result = await harness.Build().Handle(CreateCommand(customer.Id, (item.Id, 1m)), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("NumberingSeriesMissing", result.Error.Code);
        }

        /// <summary>
        /// Nothing is written when a line cannot be resolved. A half built order is worse than
        /// none, because the customer has already been told something different.
        /// </summary>
        [Fact]
        public async Task A_refused_order_is_never_created()
        {
            var item = CreateItem();
            var customer = new Customer("C-1", "Sepehr Trading", CustomerType.Organization);
            var harness = new Harness(customer, [item], null, CreateSeries());

            await harness.Build().Handle(CreateCommand(customer.Id, (item.Id, 1m)), Token);

            Assert.Null(harness.Created);
            harness.Orders.Verify(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
