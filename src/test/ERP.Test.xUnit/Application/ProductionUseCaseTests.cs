using ERP.Application.UseCases.Modules.ProductionManagement.Commands.CompleteProductionOrder;
using ERP.Application.UseCases.Modules.ProductionManagement.Commands.CreateProductionOrder;
using ERP.Application.UseCases.Modules.ProductionManagement.Commands.IssueMaterial;
using ERP.Application.UseCases.Modules.ProductionManagement.Commands.RecordQualityCheck;
using ERP.Application.UseCases.Modules.ProductionManagement.Commands.ReleaseProductionOrder;
using ERP.Application.UseCases.Modules.ProductionManagement.DTOs;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Entities.Modules.WarehouseManagement;
using ERP.Domain.Entities.Users;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// Making things.
    /// <para>
    /// What is really being tested is that production costs something. An order that consumes no
    /// material and receives finished goods at no value is the failure mode: it makes every
    /// product look free and every margin look wonderful.
    /// </para>
    /// </summary>
    public sealed class ProductionUseCaseTests
    {
        private static readonly Guid Unit = Guid.NewGuid();
        private static readonly DateOnly Today = new(2026, 5, 10);
        private static readonly DateTimeOffset Now = new(2026, 5, 10, 9, 0, 0, TimeSpan.Zero);

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private sealed class FixedClock(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private static Item CreateProduct(bool manufactured = true)
        {
            var item = new Item(new ItemGroup("G-1", "Assemblies"), ItemType.Product);
            item.Update("SKU-P", "Bracket");
            item.SetUsage(isPurchasable: false, isSellable: true, isManufactured: manufactured);
            item.SetBaseUnitOfMeasure(Unit);
            return item;
        }

        private static Item CreateComponent(string sku, string name)
        {
            var item = new Item(new ItemGroup("G-2", "Parts"), ItemType.Part);
            item.Update(sku, name);
            item.SetUsage(isPurchasable: true, isSellable: false, isManufactured: false);
            item.SetBaseUnitOfMeasure(Unit);
            return item;
        }

        private static Warehouse CreateWarehouse() =>
            new("W-1", "Main", WarehouseType.Product,
                new Address("Iran", "Tehran", "Tehran", "District 3", "Mirdamad", "12", "1234567890"));

        private static BillOfMaterials CreateBill(Item product, Item component, decimal quantityPerUnit)
        {
            var bill = new BillOfMaterials(product, "BOM-1", "Bracket recipe",
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));

            bill.AddComponent(new BillOfMaterialItem(bill, component, 1, quantityPerUnit, Unit, "pcs"));
            bill.Approve();
            bill.MarkAsDefault();

            return bill;
        }

        private sealed class CreateHarness
        {
            public Mock<IItemRepository> Items { get; } = new();
            public Mock<IBillOfMaterialsRepository> Bills { get; } = new();
            public Mock<IProductionOrderRepository> Orders { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public ProductionOrder? Created { get; private set; }

            public CreateHarness(Item? product, BillOfMaterials? bill)
            {
                Items.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(product);

                Bills.Setup(r => r.GetEffectiveForItemAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(bill);

                Bills.Setup(r => r.GetWithComponentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(bill);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("ProductionOrder", paddingLength: 5));

                Orders.Setup(r => r.CreateAsync(It.IsAny<ProductionOrder>(), It.IsAny<CancellationToken>()))
                    .Callback<ProductionOrder, CancellationToken>((order, _) => Created = order)
                    .Returns(Task.CompletedTask);
            }

            public CreateProductionOrderCommandHandler Build() => new(
                Items.Object, Bills.Object, Orders.Object, Series.Object);
        }

        private static CreateProductionOrderCommand CreateCommand(Guid itemId, decimal quantity = 10m) => new()
        {
            ItemId = itemId,
            Quantity = quantity,
            CurrencyCode = "IRR",
            PlannedStartDate = Today,
            DueDate = Today.AddDays(7),
            WarehouseId = Guid.NewGuid()
        };

        [Fact]
        public async Task The_recipe_is_pinned_to_the_order_as_it_is_raised()
        {
            // Bills get revised. An order that looked the recipe up later would be costed against
            // a formula it was never built to.
            var product = CreateProduct();
            var bill = CreateBill(product, CreateComponent("SKU-C", "Plate"), 2m);
            var harness = new CreateHarness(product, bill);

            var result = await harness.Build().Handle(CreateCommand(product.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(bill.Id, result.Value.BillOfMaterialsId);
            Assert.Equal(ProductionOrderStatus.Planned, result.Value.Status);
        }

        [Fact]
        public async Task An_item_nobody_makes_cannot_be_the_output_of_a_works_order()
        {
            var product = CreateProduct(manufactured: false);
            var harness = new CreateHarness(product, null);

            var result = await harness.Build().Handle(CreateCommand(product.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("ItemIsNotManufactured", result.Error.Code);
        }

        [Fact]
        public async Task An_item_with_no_approved_recipe_cannot_be_ordered()
        {
            var product = CreateProduct();
            var harness = new CreateHarness(product, null);

            var result = await harness.Build().Handle(CreateCommand(product.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("NoBillOfMaterials", result.Error.Code);
        }

        /// <summary>
        /// A works order wired to real inventory and a real recipe, so the cost that ends up on
        /// the finished goods can be followed rather than asserted about.
        /// </summary>
        private sealed class ShopFloor
        {
            public Mock<IProductionOrderRepository> Orders { get; } = new();
            public Mock<IBillOfMaterialsRepository> Bills { get; } = new();
            public Mock<IItemRepository> Items { get; } = new();
            public Mock<IItemInventoryRepository> Inventories { get; } = new();
            public Mock<IStockLedgerEntryRepository> Ledger { get; } = new();
            public Mock<IMaterialIssueRepository> Issues { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public List<StockLedgerEntry> Entries { get; } = [];

            public ProductionOrder Order { get; }
            public Item Product { get; }
            public Item Component { get; }
            public Warehouse Warehouse { get; }
            public ItemInventory ComponentStock { get; }
            public ItemInventory ProductStock { get; }
            public decimal AlreadyIssued { get; set; }

            public ShopFloor(decimal orderQuantity = 10m, decimal perUnit = 2m, decimal componentOnHand = 100m, decimal componentUnitCost = 300m)
            {
                Product = CreateProduct();
                Component = CreateComponent("SKU-C", "Plate");
                Warehouse = CreateWarehouse();

                var bill = CreateBill(Product, Component, perUnit);

                Order = new ProductionOrder("PO-1", Product, orderQuantity, "IRR");
                Order.SetTargets(Warehouse.Id, null, Unit);
                Order.Plan(bill.Id, Guid.Empty, Today, Today.AddDays(7));

                ComponentStock = new ItemInventory(Component, Warehouse, "IRR");
                if (componentOnHand > 0m)
                    ComponentStock.Increase(componentOnHand, Money.Create(componentOnHand * componentUnitCost, "IRR"));

                ProductStock = new ItemInventory(Product, Warehouse, "IRR");

                Orders.Setup(r => r.GetWithTasksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Order);
                Bills.Setup(r => r.GetWithComponentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(bill);
                Items.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([Component]);

                Inventories.Setup(r => r.GetForUpdateAsync(Component.Id, It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ComponentStock);

                Inventories.Setup(r => r.GetForUpdateAsync(Product.Id, It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ProductStock);

                Issues.Setup(r => r.GetIssuedQuantityAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => AlreadyIssued);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("MaterialIssue", paddingLength: 5));

                Ledger.Setup(r => r.CreateAsync(It.IsAny<StockLedgerEntry>(), It.IsAny<CancellationToken>()))
                    .Callback<StockLedgerEntry, CancellationToken>((entry, _) => Entries.Add(entry))
                    .Returns(Task.CompletedTask);
            }

            public ReleaseProductionOrderCommandHandler Releaser() => new(Orders.Object);

            public IssueMaterialCommandHandler Issuer() => new(
                Orders.Object, Bills.Object, Items.Object, Inventories.Object, Ledger.Object, Issues.Object, Series.Object);

            public CompleteProductionOrderCommandHandler Completer() => new(
                Orders.Object, Inventories.Object, Ledger.Object, new FixedClock(Now));
        }

        [Fact]
        public async Task Releasing_and_starting_can_happen_in_one_step()
        {
            var shop = new ShopFloor();

            var result = await shop.Releaser().Handle(
                new ReleaseProductionOrderCommand { ProductionOrderId = shop.Order.Id, StartImmediately = true },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(ProductionOrderStatus.InProgress, result.Value.Status);
        }

        [Fact]
        public async Task An_order_nobody_planned_cannot_be_released()
        {
            var product = CreateProduct();
            var order = new ProductionOrder("PO-2", product, 10m, "IRR");

            var orders = new Mock<IProductionOrderRepository>();
            orders.Setup(r => r.GetWithTasksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(order);

            var result = await new ReleaseProductionOrderCommandHandler(orders.Object).Handle(
                new ReleaseProductionOrderCommand { ProductionOrderId = order.Id },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("OrderCannotBeChanged", result.Error.Code);
        }

        private static IssueMaterialCommand IssueCommand(ShopFloor shop, decimal? quantity = null) => new()
        {
            ProductionOrderId = shop.Order.Id,
            WarehouseId = shop.Warehouse.Id,
            IssueDate = Today,
            Lines = quantity is null
                ? []
                : [new MaterialIssueLineDto { ComponentId = shop.Component.Id, Quantity = quantity.Value }]
        };

        [Fact]
        public async Task Naming_no_lines_issues_what_the_recipe_still_calls_for()
        {
            var shop = new ShopFloor(orderQuantity: 10m, perUnit: 2m);
            shop.Order.Release();

            var result = await shop.Issuer().Handle(IssueCommand(shop), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(20m, result.Value.Lines[0].IssuedQuantity);
        }

        [Fact]
        public async Task Issuing_takes_the_stock_writes_the_ledger_and_charges_the_order_together()
        {
            var shop = new ShopFloor(orderQuantity: 10m, perUnit: 2m, componentOnHand: 100m, componentUnitCost: 300m);
            shop.Order.Release();

            var result = await shop.Issuer().Handle(IssueCommand(shop), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(80m, shop.ComponentStock.OnHandQuantity);
            Assert.Equal(6_000m, shop.Order.MaterialCost.Amount);

            var entry = Assert.Single(shop.Entries);
            Assert.Equal(StockMovementType.ProductionIssue, entry.MovementType);
            Assert.Equal(-20m, entry.QuantityChange);
        }

        [Fact]
        public async Task A_second_issue_for_the_same_shortage_is_refused()
        {
            // The commonest way an order quietly eats twice the material it needed, and nothing
            // downstream would ever show it.
            var shop = new ShopFloor(orderQuantity: 10m, perUnit: 2m);
            shop.Order.Release();
            shop.AlreadyIssued = 20m;

            var result = await shop.Issuer().Handle(IssueCommand(shop, quantity: 5m), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("MoreIssuedThanRequired", result.Error.Code);
        }

        [Fact]
        public async Task A_component_the_product_is_not_made_of_cannot_be_charged_to_it()
        {
            var shop = new ShopFloor();
            shop.Order.Release();

            var stranger = CreateComponent("SKU-X", "Unrelated");
            shop.Items.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([stranger]);

            var result = await shop.Issuer().Handle(
                new IssueMaterialCommand
                {
                    ProductionOrderId = shop.Order.Id,
                    WarehouseId = shop.Warehouse.Id,
                    IssueDate = Today,
                    Lines = [new MaterialIssueLineDto { ComponentId = stranger.Id, Quantity = 1m }]
                },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("ComponentIsNotOnTheBill", result.Error.Code);
        }

        [Fact]
        public async Task Material_cannot_be_issued_to_an_order_that_was_never_released()
        {
            var shop = new ShopFloor();

            var result = await shop.Issuer().Handle(IssueCommand(shop), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("MaterialCannotBeIssued", result.Error.Code);
            Assert.Equal(100m, shop.ComponentStock.OnHandQuantity);
        }

        [Fact]
        public async Task Issuing_more_than_the_warehouse_holds_is_refused()
        {
            var shop = new ShopFloor(orderQuantity: 10m, perUnit: 2m, componentOnHand: 5m);
            shop.Order.Release();

            var result = await shop.Issuer().Handle(IssueCommand(shop), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("InsufficientStock", result.Error.Code);
        }

        [Fact]
        public async Task Finished_goods_are_valued_at_what_the_run_cost()
        {
            var shop = new ShopFloor(orderQuantity: 10m, perUnit: 2m, componentOnHand: 100m, componentUnitCost: 300m);
            shop.Order.Release();

            await shop.Issuer().Handle(IssueCommand(shop), Token);
            shop.Order.Start();

            var result = await shop.Completer().Handle(
                new CompleteProductionOrderCommand
                {
                    ProductionOrderId = shop.Order.Id,
                    ProducedQuantity = 10m,
                    LabourCost = 2_000m,
                    OverheadCost = 1_000m
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(ProductionOrderStatus.Completed, result.Value.Status);

            // 6,000 of material plus 2,000 labour plus 1,000 overhead over ten good units.
            Assert.Equal(900m, result.Value.UnitCost);
            Assert.Equal(10m, shop.ProductStock.OnHandQuantity);
            Assert.Equal(9_000m, shop.ProductStock.TotalValue.Amount);
        }

        [Fact]
        public async Task Scrap_is_costed_but_never_becomes_stock()
        {
            // What makes a bad run show up as an expensive product rather than disappearing into
            // a quantity nobody has.
            var shop = new ShopFloor(orderQuantity: 10m, perUnit: 2m, componentOnHand: 100m, componentUnitCost: 300m);
            shop.Order.Release();

            await shop.Issuer().Handle(IssueCommand(shop), Token);
            shop.Order.Start();

            var result = await shop.Completer().Handle(
                new CompleteProductionOrderCommand
                {
                    ProductionOrderId = shop.Order.Id,
                    ProducedQuantity = 8m,
                    ScrappedQuantity = 2m
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(8m, shop.ProductStock.OnHandQuantity);

            // The whole 6,000 of material is carried by the eight good units, not by ten.
            Assert.Equal(750m, result.Value.UnitCost);
            Assert.Equal(6_000m, shop.ProductStock.TotalValue.Amount);
        }

        [Fact]
        public async Task An_order_cannot_be_closed_without_saying_what_came_off_the_line()
        {
            var shop = new ShopFloor();
            shop.Order.Release();
            shop.Order.Start();

            var result = await shop.Completer().Handle(
                new CompleteProductionOrderCommand { ProductionOrderId = shop.Order.Id },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("NothingWasProduced", result.Error.Code);
        }

        [Fact]
        public async Task More_cannot_be_produced_than_was_ordered()
        {
            var shop = new ShopFloor(orderQuantity: 10m);
            shop.Order.Release();
            shop.Order.Start();

            var result = await shop.Completer().Handle(
                new CompleteProductionOrderCommand { ProductionOrderId = shop.Order.Id, ProducedQuantity = 11m },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("OrderCannotBeChanged", result.Error.Code);
        }

        private static Human CreateInspector() =>
            new(new User("hasti.karimi", "hash"), "Hasti", "Karimi", HumanGender.Female, new DateOnly(1994, 3, 1), maritalStatus: false);

        private static QualityParameter CreateParameter(decimal min, decimal max, bool active = true)
        {
            var parameter = new QualityParameter("QP-1", "Thickness", QualityParameterType.Numeric);
            parameter.SetTolerance(targetValue: (min + max) / 2m, minimumValue: min, maximumValue: max, unitOfMeasure: "mm");

            if (!active)
                parameter.Deactivate();

            return parameter;
        }

        private sealed class QualityHarness
        {
            public Mock<IHumanRepository> Humans { get; } = new();
            public Mock<IProductionOrderRepository> Orders { get; } = new();
            public Mock<IQualityControlCheckRepository> Checks { get; } = new();
            public QualityControlCheck? Created { get; private set; }

            public QualityHarness(Human? inspector, params QualityParameter[] parameters)
            {
                Humans.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(inspector);

                Checks.Setup(r => r.GetParametersAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(parameters);

                Checks.Setup(r => r.CreateAsync(It.IsAny<QualityControlCheck>(), It.IsAny<CancellationToken>()))
                    .Callback<QualityControlCheck, CancellationToken>((check, _) => Created = check)
                    .Returns(Task.CompletedTask);
            }

            public RecordQualityCheckCommandHandler Build() => new(Humans.Object, Orders.Object, Checks.Object);
        }

        [Fact]
        public async Task A_reading_inside_its_tolerance_passes()
        {
            var parameter = CreateParameter(9.5m, 10.5m);
            var harness = new QualityHarness(CreateInspector(), parameter);

            var result = await harness.Build().Handle(
                new RecordQualityCheckCommand
                {
                    InspectorHumanId = Guid.NewGuid(),
                    InspectedQuantity = 20m,
                    PassedQuantity = 20m,
                    Measurements = [new QualityMeasurementDto { ParameterId = parameter.Id, NumericValue = 10m }]
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value.Passed);
            Assert.True(result.Value.Measurements[0].IsWithinTolerance);
        }

        [Fact]
        public async Task A_pass_cannot_be_recorded_over_a_reading_that_was_out_of_tolerance()
        {
            // The request says what was measured; the parameter says whether that is acceptable.
            // Nobody gets to declare a batch good over a measurement that says otherwise.
            var parameter = CreateParameter(9.5m, 10.5m);
            var harness = new QualityHarness(CreateInspector(), parameter);

            var result = await harness.Build().Handle(
                new RecordQualityCheckCommand
                {
                    InspectorHumanId = Guid.NewGuid(),
                    InspectedQuantity = 20m,
                    PassedQuantity = 20m,
                    Measurements = [new QualityMeasurementDto { ParameterId = parameter.Id, NumericValue = 12m, IsAcceptable = true }]
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.False(result.Value.Passed);
        }

        [Fact]
        public async Task Some_units_failing_fails_the_check_whatever_the_readings_said()
        {
            var parameter = CreateParameter(9.5m, 10.5m);
            var harness = new QualityHarness(CreateInspector(), parameter);

            var result = await harness.Build().Handle(
                new RecordQualityCheckCommand
                {
                    InspectorHumanId = Guid.NewGuid(),
                    InspectedQuantity = 20m,
                    PassedQuantity = 18m,
                    Measurements = [new QualityMeasurementDto { ParameterId = parameter.Id, NumericValue = 10m }]
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.False(result.Value.Passed);
            Assert.Equal(2m, result.Value.FailedQuantity);
            Assert.Equal(90m, result.Value.PassRate);
        }

        [Fact]
        public async Task A_retired_parameter_is_refused_rather_than_judged_against()
        {
            var parameter = CreateParameter(9.5m, 10.5m, active: false);
            var harness = new QualityHarness(CreateInspector(), parameter);

            var result = await harness.Build().Handle(
                new RecordQualityCheckCommand
                {
                    InspectorHumanId = Guid.NewGuid(),
                    InspectedQuantity = 20m,
                    PassedQuantity = 20m,
                    Measurements = [new QualityMeasurementDto { ParameterId = parameter.Id, NumericValue = 10m }]
                },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("QualityCheckCannotBeRecorded", result.Error.Code);
        }

        [Fact]
        public async Task An_inspection_nobody_carried_out_is_not_recorded()
        {
            var harness = new QualityHarness(null);

            var result = await harness.Build().Handle(
                new RecordQualityCheckCommand { InspectorHumanId = Guid.NewGuid(), InspectedQuantity = 20m, PassedQuantity = 20m },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("RecordNotFound", result.Error.Code);
        }
    }
}
