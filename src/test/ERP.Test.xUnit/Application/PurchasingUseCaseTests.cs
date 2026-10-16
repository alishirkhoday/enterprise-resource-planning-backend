using ERP.Application.UseCases.Modules.InventoryManagement.Commands.ApprovePurchaseOrder;
using ERP.Application.UseCases.Modules.InventoryManagement.Commands.CreatePurchaseOrder;
using ERP.Application.UseCases.Modules.InventoryManagement.Commands.ReceiveGoods;
using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.WarehouseManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// Ordering and receiving. The receipt is the interesting one: it moves three things at once,
    /// and any two of them without the third leaves the books lying about what is on the shelf.
    /// </summary>
    public sealed class PurchasingUseCaseTests
    {
        private static readonly Guid Unit = Guid.NewGuid();
        private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private sealed class FixedClock(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private static Supplier CreateSupplier() => new("S-1", "Pars Fasteners", SupplierType.Organization, "sales@pars.example");

        private static Item CreateItem(bool isPurchasable = true)
        {
            var item = new Item(new ItemGroup("G-1", "Fasteners"), ItemType.Part);
            item.Update("SKU-1", "Bolt");
            item.SetUsage(isPurchasable, isSellable: true, isManufactured: false);
            return item;
        }

        private static Warehouse CreateWarehouse() =>
            new("W-1", "Main", WarehouseType.Product,
                new Address("Iran", "Tehran", "Tehran", "District 3", "Mirdamad", "12", "1234567890"));

        private sealed class CreateHarness
        {
            public Mock<ISupplierRepository> Suppliers { get; } = new();
            public Mock<IItemRepository> Items { get; } = new();
            public Mock<IPurchaseContractRepository> Contracts { get; } = new();
            public Mock<IPurchaseOrderRepository> Orders { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public PurchaseOrder? Created { get; private set; }

            public CreateHarness(Supplier? supplier, Item item, PurchaseContract? contract = null)
            {
                Suppliers.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
                Items.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([item]);

                Contracts.Setup(r => r.GetEffectiveForItemAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(contract);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("PurchaseOrder", paddingLength: 5));

                Orders.Setup(r => r.CreateAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
                    .Callback<PurchaseOrder, CancellationToken>((order, _) => Created = order)
                    .Returns(Task.CompletedTask);
            }

            public CreatePurchaseOrderCommandHandler Build() => new(
                Suppliers.Object, Items.Object, Contracts.Object, Orders.Object, Series.Object, new FixedClock(Now));
        }

        private static CreatePurchaseOrderCommand CreateCommand(Guid supplierId, Guid itemId, decimal? unitPrice) => new()
        {
            SupplierId = supplierId,
            CurrencyCode = "IRR",
            Lines = [new PurchaseOrderLineDto { ItemId = itemId, Quantity = 100m, UnitOfMeasureId = Unit, UnitPrice = unitPrice }]
        };

        private static PurchaseContract CreateContract(Supplier supplier, Item item, decimal agreedPrice)
        {
            var contract = new PurchaseContract("PC-1", "Fastener supply", supplier,
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "IRR");

            contract.AddItem(new PurchaseContractItem(contract, item, Money.Create(agreedPrice, "IRR"), Unit));
            contract.Activate(new DateOnly(2026, 1, 1));
            return contract;
        }

        [Fact]
        public async Task An_order_uses_the_price_on_the_request_when_one_is_given()
        {
            var supplier = CreateSupplier();
            var item = CreateItem();
            var harness = new CreateHarness(supplier, item);

            var result = await harness.Build().Handle(CreateCommand(supplier.Id, item.Id, 15_000m), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(1_500_000m, result.Value.SubTotal);
        }

        /// <summary>
        /// The price was negotiated once and should not be retyped, and mistyped, on every order.
        /// </summary>
        [Fact]
        public async Task A_line_with_no_price_falls_back_to_the_framework_agreement()
        {
            var supplier = CreateSupplier();
            var item = CreateItem();
            var harness = new CreateHarness(supplier, item, CreateContract(supplier, item, 12_000m));

            var result = await harness.Build().Handle(CreateCommand(supplier.Id, item.Id, null), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(1_200_000m, result.Value.SubTotal);
        }

        [Fact]
        public async Task A_line_with_no_price_and_no_agreement_is_refused()
        {
            var supplier = CreateSupplier();
            var item = CreateItem();
            var harness = new CreateHarness(supplier, item);

            var result = await harness.Build().Handle(CreateCommand(supplier.Id, item.Id, null), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("NoPriceForItem", result.Error.Code);
        }

        [Fact]
        public async Task An_item_that_is_not_purchasable_is_refused()
        {
            var supplier = CreateSupplier();
            var item = CreateItem(isPurchasable: false);
            var harness = new CreateHarness(supplier, item);

            var result = await harness.Build().Handle(CreateCommand(supplier.Id, item.Id, 15_000m), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("ItemIsNotPurchasable", result.Error.Code);
        }

        private static PurchaseOrder CreateSubmittedOrder(Supplier supplier, Item item, decimal quantity = 100m)
        {
            var order = new PurchaseOrder("PO-1", supplier, "IRR");
            order.AddItem(new PurchaseOrderItem(order, item, 1, quantity, Unit, Money.Create(15_000m, "IRR")));
            order.Submit();
            return order;
        }

        private static Mock<IPurchaseOrderRepository> OrdersReturning(PurchaseOrder order)
        {
            var orders = new Mock<IPurchaseOrderRepository>();
            orders.Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(order);
            return orders;
        }

        [Fact]
        public async Task Approving_a_submitted_order_records_the_fact()
        {
            var order = CreateSubmittedOrder(CreateSupplier(), CreateItem());
            var orders = OrdersReturning(order);

            var result = await new ApprovePurchaseOrderCommandHandler(orders.Object).Handle(
                new ApprovePurchaseOrderCommand { PurchaseOrderId = order.Id, IsApproved = true },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(PurchaseOrderStatus.Approved, result.Value.Status);
            Assert.Contains(order.DomainEvents, e => e is PurchaseOrderApproved);
        }

        [Fact]
        public async Task An_order_that_was_never_submitted_cannot_be_approved()
        {
            var supplier = CreateSupplier();
            var order = new PurchaseOrder("PO-1", supplier, "IRR");
            order.AddItem(new PurchaseOrderItem(order, CreateItem(), 1, 100m, Unit, Money.Create(15_000m, "IRR")));

            var orders = OrdersReturning(order);

            var result = await new ApprovePurchaseOrderCommandHandler(orders.Object).Handle(
                new ApprovePurchaseOrderCommand { PurchaseOrderId = order.Id, IsApproved = true },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("OrderCannotBeDecided", result.Error.Code);
        }

        private sealed class ReceiveHarness
        {
            public Mock<IPurchaseOrderRepository> Orders { get; } = new();
            public Mock<IItemInventoryRepository> Inventories { get; } = new();
            public Mock<IStockLedgerEntryRepository> Ledger { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public List<StockLedgerEntry> Written { get; } = [];

            public ReceiveHarness(PurchaseOrder order, ItemInventory? inventory)
            {
                Orders.Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(order);

                Inventories.Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(inventory);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("GoodsReceipt", paddingLength: 5));

                Ledger.Setup(r => r.CreateAsync(It.IsAny<StockLedgerEntry>(), It.IsAny<CancellationToken>()))
                    .Callback<StockLedgerEntry, CancellationToken>((entry, _) => Written.Add(entry))
                    .Returns(Task.CompletedTask);
            }

            public ReceiveGoodsCommandHandler Build() => new(
                Orders.Object, Inventories.Object, Ledger.Object, Series.Object, new FixedClock(Now));
        }

        private static PurchaseOrder CreateApprovedOrder(Supplier supplier, Item item, decimal quantity = 100m)
        {
            var order = CreateSubmittedOrder(supplier, item, quantity);
            order.Approve();
            return order;
        }

        private static ReceiveGoodsCommand ReceiveCommand(
            PurchaseOrder order,
            Guid warehouseId,
            decimal received,
            decimal rejected = 0m) => new()
        {
            PurchaseOrderId = order.Id,
            WarehouseId = warehouseId,
            ReceiptDate = new DateOnly(2026, 3, 2),
            Lines =
            [
                new ReceiveGoodsLineDto
                {
                    PurchaseOrderItemId = order.Items[0].Id,
                    ReceivedQuantity = received,
                    RejectedQuantity = rejected,
                    RejectionReason = rejected > 0m ? "Failed inspection." : null
                }
            ]
        };

        /// <summary>
        /// The receipt, the order lines it satisfies and the stock it puts on the shelf all move
        /// together, or the books say something that is not on the shelf.
        /// </summary>
        [Fact]
        public async Task A_delivery_raises_the_stock_and_satisfies_the_order_line()
        {
            var supplier = CreateSupplier();
            var item = CreateItem();
            var warehouse = CreateWarehouse();
            var order = CreateApprovedOrder(supplier, item);
            var inventory = new ItemInventory(item, warehouse, "IRR");

            var harness = new ReceiveHarness(order, inventory);

            var result = await harness.Build().Handle(ReceiveCommand(order, warehouse.Id, 100m), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(100m, inventory.OnHandQuantity);
            Assert.Equal(100m, order.Items[0].ReceivedQuantity);
            Assert.Single(harness.Written);
            Assert.Equal(StockMovementType.PurchaseReceipt, harness.Written[0].MovementType);
        }

        /// <summary>
        /// Rejected goods arrived but never became stock.
        /// </summary>
        [Fact]
        public async Task Only_what_passes_inspection_reaches_the_shelf()
        {
            var supplier = CreateSupplier();
            var item = CreateItem();
            var warehouse = CreateWarehouse();
            var order = CreateApprovedOrder(supplier, item);
            var inventory = new ItemInventory(item, warehouse, "IRR");

            var harness = new ReceiveHarness(order, inventory);

            var result = await harness.Build().Handle(ReceiveCommand(order, warehouse.Id, 100m, rejected: 20m), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(80m, inventory.OnHandQuantity);
            Assert.Equal(80m, result.Value.Lines[0].AcceptedQuantity);
            Assert.Equal(20m, result.Value.Lines[0].RejectedQuantity);
        }

        /// <summary>
        /// Either a duplicate entry or a supplier shipping more than was ordered. Both are
        /// decisions for a person, not something to absorb silently.
        /// </summary>
        [Fact]
        public async Task More_than_the_order_still_has_outstanding_is_refused()
        {
            var supplier = CreateSupplier();
            var item = CreateItem();
            var warehouse = CreateWarehouse();
            var order = CreateApprovedOrder(supplier, item, quantity: 100m);
            var inventory = new ItemInventory(item, warehouse, "IRR");

            var harness = new ReceiveHarness(order, inventory);

            var result = await harness.Build().Handle(ReceiveCommand(order, warehouse.Id, 150m), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("MoreReceivedThanOrdered", result.Error.Code);
            Assert.Equal(0m, inventory.OnHandQuantity);
        }

        [Fact]
        public async Task An_order_that_was_never_approved_cannot_take_a_delivery()
        {
            var supplier = CreateSupplier();
            var item = CreateItem();
            var warehouse = CreateWarehouse();
            var order = CreateSubmittedOrder(supplier, item);

            var harness = new ReceiveHarness(order, new ItemInventory(item, warehouse, "IRR"));

            var result = await harness.Build().Handle(ReceiveCommand(order, warehouse.Id, 100m), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("OrderIsNotReceivable", result.Error.Code);
        }

        [Fact]
        public async Task A_warehouse_with_no_inventory_record_is_refused()
        {
            var supplier = CreateSupplier();
            var item = CreateItem();
            var order = CreateApprovedOrder(supplier, item);

            var harness = new ReceiveHarness(order, inventory: null);

            var result = await harness.Build().Handle(ReceiveCommand(order, Guid.NewGuid(), 100m), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("NoInventoryRecord", result.Error.Code);
        }

        /// <summary>
        /// Part of an order is a normal delivery, and the order has to stay open for the rest.
        /// </summary>
        [Fact]
        public async Task A_partial_delivery_leaves_the_order_open()
        {
            var supplier = CreateSupplier();
            var item = CreateItem();
            var warehouse = CreateWarehouse();
            var order = CreateApprovedOrder(supplier, item, quantity: 100m);
            var inventory = new ItemInventory(item, warehouse, "IRR");

            var harness = new ReceiveHarness(order, inventory);

            var result = await harness.Build().Handle(ReceiveCommand(order, warehouse.Id, 40m), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(40m, inventory.OnHandQuantity);
            Assert.Equal(PurchaseOrderStatus.PartiallyReceived, order.Status);
        }
    }
}
