using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.AssessWarrantyClaim;
using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.CompleteServiceOrder;
using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.RaiseServiceOrder;
using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.RegisterWarrantyClaim;
using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.WarehouseManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.AfterSalesServiceManagement;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// Who pays for a visit, and what leaves the shelf for it. Both are decisions that quietly
    /// cost money when they are made in the wrong place or at the wrong time.
    /// </summary>
    public sealed class AfterSalesUseCaseTests
    {
        private static readonly Guid Unit = Guid.NewGuid();
        private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private sealed class FixedClock(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private static Customer CreateCustomer() => new("C-1", "Sepehr Trading", CustomerType.Organization);

        private static Warehouse CreateWarehouse() =>
            new("W-1", "Main", WarehouseType.Product, new Address("Iran", "Tehran", "Tehran", "District 3", "Mirdamad", "12", "1234567890"));

        private static Item CreateItem(string name = "Pump", ItemTrackingType tracking = ItemTrackingType.Serial)
        {
            var item = new Item(new ItemGroup("G-1", "Pumps"), ItemType.Product);
            item.Update("SKU-1", name);
            item.SetTracking(tracking);
            return item;
        }

        private static SerialNumber CreateUnit(Item item, int? warrantyMonths)
        {
            var unit = new SerialNumber(item, "SN-0001");
            unit.Receive(Guid.NewGuid(), new DateOnly(2025, 12, 1));
            unit.Sell(new DateOnly(2026, 1, 1), warrantyMonths: warrantyMonths);
            return unit;
        }

        private static ServiceContract CreateContract(Customer customer, Item item, int? includedVisits = null)
        {
            var contract = new ServiceContract(
                "SC-1",
                "Annual cover",
                customer,
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
                ServiceCoverageType.Comprehensive,
                Money.Create(50_000_000m, "IRR"));

            contract.SetServiceLevel(responseHours: 4, resolutionHours: 24);
            contract.SetIncludedVisits(includedVisits);
            contract.Cover(new ServiceContractCoverage(contract, item));
            contract.Activate(new DateOnly(2026, 1, 1));
            return contract;
        }

        private sealed class RaiseHarness
        {
            public Mock<ICustomerRepository> Customers { get; } = new();
            public Mock<ISerialNumberRepository> SerialNumbers { get; } = new();
            public Mock<IServiceContractRepository> Contracts { get; } = new();
            public Mock<IServiceOrderRepository> Orders { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public ServiceOrder? Created { get; private set; }

            public RaiseHarness(Customer customer, SerialNumber? unit, ServiceContract? contract)
            {
                Customers.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(customer);
                SerialNumbers.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(unit);

                Contracts.Setup(r => r.GetCoveringAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(contract);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("ServiceOrder", paddingLength: 5));

                Orders.Setup(r => r.CreateAsync(It.IsAny<ServiceOrder>(), It.IsAny<CancellationToken>()))
                    .Callback<ServiceOrder, CancellationToken>((order, _) => Created = order)
                    .Returns(Task.CompletedTask);
            }

            public RaiseServiceOrderCommandHandler Build() => new(
                Customers.Object, SerialNumbers.Object, Contracts.Object, Orders.Object, Series.Object, new FixedClock(Now));
        }

        private static RaiseServiceOrderCommand RaiseCommand(Guid customerId, Guid? itemId = null, Guid? serialNumberId = null) => new()
        {
            CustomerId = customerId,
            ReportedFault = "Will not start.",
            CurrencyCode = "IRR",
            ItemId = itemId,
            SerialNumberId = serialNumberId,
            WarehouseId = Guid.NewGuid()
        };

        /// <summary>
        /// A unit still in warranty is the manufacturer's cost, so it wins even where a paid
        /// contract also covers the equipment.
        /// </summary>
        [Fact]
        public async Task A_unit_in_warranty_is_covered_by_warranty_even_under_a_contract()
        {
            var customer = CreateCustomer();
            var item = CreateItem();
            var unit = CreateUnit(item, warrantyMonths: 24);
            var harness = new RaiseHarness(customer, unit, CreateContract(customer, item));

            var result = await harness.Build().Handle(RaiseCommand(customer.Id, item.Id, unit.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(ServiceOrderType.Warranty, result.Value.Type);
            Assert.False(result.Value.IsChargeable);
        }

        [Fact]
        public async Task A_unit_out_of_warranty_falls_to_the_contract()
        {
            var customer = CreateCustomer();
            var item = CreateItem();
            var unit = CreateUnit(item, warrantyMonths: 1);
            var contract = CreateContract(customer, item);
            var harness = new RaiseHarness(customer, unit, contract);

            var result = await harness.Build().Handle(RaiseCommand(customer.Id, item.Id, unit.Id), Token);

            Assert.Equal(ServiceOrderType.Contract, result.Value.Type);
            Assert.Equal(contract.Id, result.Value.ServiceContractId);
        }

        /// <summary>
        /// Counting included visits is only worth doing if running out changes something, and what
        /// it changes is who pays for the next one.
        /// </summary>
        [Fact]
        public async Task A_contract_with_no_visits_left_makes_the_visit_chargeable()
        {
            var customer = CreateCustomer();
            var item = CreateItem();
            var contract = CreateContract(customer, item, includedVisits: 1);
            contract.RecordVisit();

            var harness = new RaiseHarness(customer, null, contract);

            var result = await harness.Build().Handle(RaiseCommand(customer.Id, item.Id), Token);

            Assert.Equal(ServiceOrderType.Chargeable, result.Value.Type);
            Assert.True(result.Value.IsChargeable);
        }

        [Fact]
        public async Task With_no_warranty_and_no_contract_the_customer_pays()
        {
            var customer = CreateCustomer();
            var harness = new RaiseHarness(customer, null, null);

            var result = await harness.Build().Handle(RaiseCommand(customer.Id, CreateItem().Id), Token);

            Assert.Equal(ServiceOrderType.Chargeable, result.Value.Type);
        }

        /// <summary>
        /// The promise is recorded as moments now, so a breach later is a comparison rather than
        /// an argument about which contract was in force at the time.
        /// </summary>
        [Fact]
        public async Task A_contract_visit_carries_the_promised_response_times()
        {
            var customer = CreateCustomer();
            var item = CreateItem();
            var harness = new RaiseHarness(customer, null, CreateContract(customer, item));

            var result = await harness.Build().Handle(RaiseCommand(customer.Id, item.Id), Token);

            Assert.NotNull(result.Value.ResponseDueAt);
            Assert.Equal(result.Value.RequestedAt.AddHours(4), result.Value.ResponseDueAt);
            Assert.Equal(result.Value.RequestedAt.AddHours(24), result.Value.ResolutionDueAt);
        }

        private sealed class CompleteHarness
        {
            public Mock<IServiceOrderRepository> Orders { get; } = new();
            public Mock<IServiceContractRepository> Contracts { get; } = new();
            public Mock<IItemRepository> Items { get; } = new();
            public Mock<IItemInventoryRepository> Inventories { get; } = new();
            public Mock<IStockLedgerEntryRepository> Ledger { get; } = new();
            public List<StockLedgerEntry> Written { get; } = [];

            public CompleteHarness(ServiceOrder order, Item part, ItemInventory? inventory, ServiceContract? contract = null)
            {
                Orders.Setup(r => r.GetWithPartsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(order);
                Items.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([part]);

                Inventories.Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(inventory);

                Contracts.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(contract);

                Ledger.Setup(r => r.CreateAsync(It.IsAny<StockLedgerEntry>(), It.IsAny<CancellationToken>()))
                    .Callback<StockLedgerEntry, CancellationToken>((entry, _) => Written.Add(entry))
                    .Returns(Task.CompletedTask);
            }

            public CompleteServiceOrderCommandHandler Build() =>
                new(Orders.Object, Contracts.Object, Items.Object, Inventories.Object, Ledger.Object);
        }

        private static ServiceOrder CreateOrder(Customer customer, ServiceOrderType type, Guid warehouseId)
        {
            var order = new ServiceOrder("SO-1", customer, type, "Will not start.", "IRR");
            order.IssuePartsFrom(warehouseId);
            order.Start();
            return order;
        }

        private static ItemInventory CreateStock(Item item, Warehouse warehouse, decimal quantity)
        {
            var inventory = new ItemInventory(item, warehouse, "IRR");
            inventory.Increase(quantity, Money.Create(quantity * 100_000m, "IRR"));
            return inventory;
        }

        private static CompleteServiceOrderCommand CompleteCommand(Guid orderId, Guid partId, decimal quantity = 2m) => new()
        {
            ServiceOrderId = orderId,
            CompletedOn = new DateOnly(2026, 3, 2),
            WorkPerformed = "Replaced the impeller.",
            LabourHours = 3m,
            LabourCharge = 900_000m,
            TravelCharge = 100_000m,
            Parts = [new ServiceOrderPartDto { ItemId = partId, Quantity = quantity, UnitPrice = 500_000m, UnitOfMeasureId = Unit }]
        };

        /// <summary>
        /// Inventory that only moves on chargeable work drifts further from reality with every
        /// warranty call, so the part leaves the shelf whoever is paying for it.
        /// </summary>
        [Theory]
        [InlineData(ServiceOrderType.Chargeable)]
        [InlineData(ServiceOrderType.Warranty)]
        public async Task A_part_leaves_stock_whoever_is_paying(ServiceOrderType type)
        {
            var warehouse = CreateWarehouse();
            var part = CreateItem("Impeller");
            var inventory = CreateStock(part, warehouse, 10m);
            var order = CreateOrder(CreateCustomer(), type, warehouse.Id);
            var harness = new CompleteHarness(order, part, inventory);

            var result = await harness.Build().Handle(CompleteCommand(order.Id, part.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(8m, inventory.OnHandQuantity);
            Assert.Single(harness.Written);
            Assert.Equal(StockMovementType.ServiceIssue, harness.Written[0].MovementType);
        }

        [Fact]
        public async Task A_warranty_part_never_reaches_the_bill()
        {
            var warehouse = CreateWarehouse();
            var part = CreateItem("Impeller");
            var order = CreateOrder(CreateCustomer(), ServiceOrderType.Warranty, warehouse.Id);
            var harness = new CompleteHarness(order, part, CreateStock(part, warehouse, 10m));

            var result = await harness.Build().Handle(CompleteCommand(order.Id, part.Id), Token);

            // Labour and travel are still recorded; only the parts total is zero.
            Assert.Equal(1_000_000m, result.Value.GrandTotal);
        }

        [Fact]
        public async Task A_chargeable_part_is_added_to_the_bill()
        {
            var warehouse = CreateWarehouse();
            var part = CreateItem("Impeller");
            var order = CreateOrder(CreateCustomer(), ServiceOrderType.Chargeable, warehouse.Id);
            var harness = new CompleteHarness(order, part, CreateStock(part, warehouse, 10m));

            var result = await harness.Build().Handle(CompleteCommand(order.Id, part.Id), Token);

            Assert.Equal(2_000_000m, result.Value.GrandTotal);
        }

        [Fact]
        public async Task Fitting_more_than_is_on_hand_is_refused()
        {
            var warehouse = CreateWarehouse();
            var part = CreateItem("Impeller");
            var order = CreateOrder(CreateCustomer(), ServiceOrderType.Chargeable, warehouse.Id);
            var harness = new CompleteHarness(order, part, CreateStock(part, warehouse, 1m));

            var result = await harness.Build().Handle(CompleteCommand(order.Id, part.Id, quantity: 5m), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("NotEnoughStockForPart", result.Error.Code);
        }

        /// <summary>
        /// A call that was never attended has consumed nothing, so the allowance is drawn down on
        /// completion rather than on raising.
        /// </summary>
        [Fact]
        public async Task Completing_a_contract_visit_draws_down_the_allowance()
        {
            var warehouse = CreateWarehouse();
            var customer = CreateCustomer();
            var part = CreateItem("Impeller");
            var contract = CreateContract(customer, part, includedVisits: 4);

            var order = new ServiceOrder("SO-1", customer, ServiceOrderType.Contract, "Will not start.", "IRR");
            order.LinkToServiceContract(contract.Id);
            order.IssuePartsFrom(warehouse.Id);
            order.Start();

            var harness = new CompleteHarness(order, part, CreateStock(part, warehouse, 10m), contract);

            await harness.Build().Handle(CompleteCommand(order.Id, part.Id), Token);

            Assert.Equal(1, contract.ConsumedVisits);
        }

        private sealed class ClaimHarness
        {
            public Mock<ICustomerRepository> Customers { get; } = new();
            public Mock<IItemRepository> Items { get; } = new();
            public Mock<ISerialNumberRepository> SerialNumbers { get; } = new();
            public Mock<IServiceContractRepository> Contracts { get; } = new();
            public Mock<IWarrantyClaimRepository> Claims { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();

            public ClaimHarness(Customer customer, Item item, ServiceContract? contract = null)
            {
                Customers.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(customer);
                Items.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(item);

                Contracts.Setup(r => r.GetCoveringAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(contract);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("WarrantyClaim", paddingLength: 5));
            }

            public RegisterWarrantyClaimCommandHandler Build() => new(
                Customers.Object, Items.Object, Contracts.Object, Claims.Object, Series.Object, new FixedClock(Now));
        }

        /// <summary>
        /// Cover is judged on when it failed, not on how long the paperwork took, so the contract
        /// is looked up against the fault date.
        /// </summary>
        [Fact]
        public async Task A_claim_is_matched_to_cover_on_the_date_it_failed()
        {
            var customer = CreateCustomer();
            var item = CreateItem();
            var contract = CreateContract(customer, item);
            var harness = new ClaimHarness(customer, item, contract);

            var result = await harness.Build().Handle(
                new RegisterWarrantyClaimCommand
                {
                    CustomerId = customer.Id,
                    ItemId = item.Id,
                    FaultOccurredOn = new DateOnly(2026, 2, 20),
                    ReportedOn = new DateOnly(2026, 3, 1),
                    FaultDescription = "Seized."
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(contract.Id, result.Value.ServiceContractId);
            Assert.Equal(WarrantyClaimStatus.Registered, result.Value.Status);

            harness.Contracts.Verify(r => r.GetCoveringAsync(
                customer.Id, item.Id, null, new DateOnly(2026, 2, 20), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task An_accepted_claim_records_how_it_will_be_honoured()
        {
            var claim = new WarrantyClaim("WC-1", CreateCustomer(), CreateItem(), new DateOnly(2026, 2, 20), new DateOnly(2026, 3, 1), "Seized.");
            var claims = new Mock<IWarrantyClaimRepository>();
            claims.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(claim);

            var result = await new AssessWarrantyClaimCommandHandler(claims.Object).Handle(
                new AssessWarrantyClaimCommand
                {
                    ClaimId = claim.Id,
                    IsApproved = true,
                    AssessedOn = new DateOnly(2026, 3, 3),
                    Resolution = WarrantyClaimResolution.Replace
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(WarrantyClaimStatus.Approved, result.Value.Status);
            Assert.Equal(WarrantyClaimResolution.Replace, result.Value.Resolution);
        }

        [Fact]
        public async Task A_claim_cannot_be_assessed_twice()
        {
            var claim = new WarrantyClaim("WC-1", CreateCustomer(), CreateItem(), new DateOnly(2026, 2, 20), new DateOnly(2026, 3, 1), "Seized.");
            claim.Approve(new DateOnly(2026, 3, 3), WarrantyClaimResolution.Repair);

            var claims = new Mock<IWarrantyClaimRepository>();
            claims.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(claim);

            var result = await new AssessWarrantyClaimCommandHandler(claims.Object).Handle(
                new AssessWarrantyClaimCommand
                {
                    ClaimId = claim.Id,
                    IsApproved = false,
                    AssessedOn = new DateOnly(2026, 3, 4),
                    RejectionReason = "Out of cover."
                },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("ClaimCannotBeDecided", result.Error.Code);
        }
    }
}
