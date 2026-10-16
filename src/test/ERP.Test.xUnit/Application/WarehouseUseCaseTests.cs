using ERP.Application.UseCases.Modules.InventoryManagement.Commands.ApproveStockCount;
using ERP.Application.UseCases.Modules.InventoryManagement.Commands.CreateStockCount;
using ERP.Application.UseCases.Modules.InventoryManagement.Commands.RecordStockCount;
using ERP.Application.UseCases.Modules.InventoryManagement.DTOs;
using ERP.Application.UseCases.Modules.InventoryManagement.Queries.GetItemStockCard;
using ERP.Application.UseCases.Modules.WarehouseManagement.Commands.ApproveStockTransfer;
using ERP.Application.UseCases.Modules.WarehouseManagement.Commands.CreateStockTransfer;
using ERP.Application.UseCases.Modules.WarehouseManagement.Commands.DispatchStockTransfer;
using ERP.Application.UseCases.Modules.WarehouseManagement.Commands.ReceiveStockTransfer;
using ERP.Application.UseCases.Modules.WarehouseManagement.DTOs;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.WarehouseManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.WarehouseManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// Transfers and counts.
    /// <para>
    /// Both are places where stock can change without anybody selling or buying anything, which
    /// makes them the two easiest places in an inventory system to lose goods quietly. The tests
    /// that matter are the ones about value: a transfer must not change what the goods are worth,
    /// and a count must not adjust anything nobody approved.
    /// </para>
    /// </summary>
    public sealed class WarehouseUseCaseTests
    {
        private static readonly Guid Unit = Guid.NewGuid();
        private static readonly DateOnly Today = new(2026, 5, 10);
        private static readonly DateTimeOffset Now = new(2026, 5, 10, 9, 0, 0, TimeSpan.Zero);

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private sealed class FixedClock(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private static Item CreateItem(string name = "Bolt", bool withUnit = true)
        {
            var item = new Item(new ItemGroup("G-1", "Fasteners"), ItemType.Part);
            item.Update("SKU-1", name);

            if (withUnit)
                item.SetBaseUnitOfMeasure(Unit);

            return item;
        }

        private static Warehouse CreateWarehouse(string code, string name) =>
            new(code, name, WarehouseType.Product,
                new Address("Iran", "Tehran", "Tehran", "District 3", "Mirdamad", "12", "1234567890"));

        private static ItemInventory CreateInventory(Item item, Warehouse warehouse, decimal quantity, decimal unitCost)
        {
            var inventory = new ItemInventory(item, warehouse, "IRR");

            if (quantity > 0m)
                inventory.Increase(quantity, Money.Create(quantity * unitCost, "IRR"));

            return inventory;
        }

        private sealed class CreateTransferHarness
        {
            public Mock<IWarehouseRepository> Warehouses { get; } = new();
            public Mock<IItemRepository> Items { get; } = new();
            public Mock<IItemInventoryRepository> Inventories { get; } = new();
            public Mock<IStockTransferOrderRepository> Transfers { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public StockTransferOrder? Created { get; private set; }

            public CreateTransferHarness(Warehouse source, Warehouse destination, Item item, ItemInventory? inventory)
            {
                Warehouses.Setup(r => r.GetByIdAsync(source.Id, It.IsAny<CancellationToken>())).ReturnsAsync(source);
                Warehouses.Setup(r => r.GetByIdAsync(destination.Id, It.IsAny<CancellationToken>())).ReturnsAsync(destination);

                Items.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([item]);

                Inventories.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(inventory);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("StockTransfer", paddingLength: 5));

                Transfers.Setup(r => r.CreateAsync(It.IsAny<StockTransferOrder>(), It.IsAny<CancellationToken>()))
                    .Callback<StockTransferOrder, CancellationToken>((transfer, _) => Created = transfer)
                    .Returns(Task.CompletedTask);
            }

            public CreateStockTransferCommandHandler Build() => new(
                Warehouses.Object, Items.Object, Inventories.Object, Transfers.Object, Series.Object);
        }

        private static CreateStockTransferCommand TransferCommand(Guid sourceId, Guid destinationId, Guid itemId, decimal quantity, decimal? unitCost = null) => new()
        {
            SourceWarehouseId = sourceId,
            DestinationWarehouseId = destinationId,
            RequestDate = Today,
            CurrencyCode = "IRR",
            Lines = [new StockTransferLineDto { ItemId = itemId, Quantity = quantity, UnitOfMeasureId = Unit, UnitCost = unitCost }]
        };

        [Fact]
        public async Task A_line_without_a_cost_takes_what_the_source_is_carrying_it_at()
        {
            var source = CreateWarehouse("W-1", "Main");
            var destination = CreateWarehouse("W-2", "Branch");
            var item = CreateItem();
            var inventory = CreateInventory(item, source, 100m, unitCost: 250m);

            var harness = new CreateTransferHarness(source, destination, item, inventory);

            var result = await harness.Build().Handle(TransferCommand(source.Id, destination.Id, item.Id, 20m), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(250m, result.Value.Lines[0].UnitCost);
        }

        [Fact]
        public async Task A_transfer_of_more_than_is_on_the_shelf_is_refused_before_anybody_walks_to_it()
        {
            var source = CreateWarehouse("W-1", "Main");
            var destination = CreateWarehouse("W-2", "Branch");
            var item = CreateItem();
            var inventory = CreateInventory(item, source, 5m, unitCost: 250m);

            var harness = new CreateTransferHarness(source, destination, item, inventory);

            var result = await harness.Build().Handle(TransferCommand(source.Id, destination.Id, item.Id, 20m), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("InsufficientStock", result.Error.Code);
        }

        [Fact]
        public async Task An_item_the_source_holds_no_record_of_cannot_be_transferred()
        {
            var source = CreateWarehouse("W-1", "Main");
            var destination = CreateWarehouse("W-2", "Branch");
            var item = CreateItem();

            var harness = new CreateTransferHarness(source, destination, item, inventory: null);

            var result = await harness.Build().Handle(TransferCommand(source.Id, destination.Id, item.Id, 20m), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("NoInventoryRecord", result.Error.Code);
        }

        /// <summary>
        /// A transfer wired up as far as the given status, with real inventory either side so the
        /// movements can be checked rather than asserted about.
        /// </summary>
        private sealed class MoveHarness
        {
            public Mock<IStockTransferOrderRepository> Transfers { get; } = new();
            public Mock<IItemInventoryRepository> Inventories { get; } = new();
            public Mock<IStockLedgerEntryRepository> Ledger { get; } = new();
            public List<StockLedgerEntry> Entries { get; } = [];

            public StockTransferOrder Transfer { get; }
            public ItemInventory SourceInventory { get; }
            public ItemInventory DestinationInventory { get; }

            public MoveHarness(decimal sourceQuantity = 100m, decimal sourceUnitCost = 250m, decimal requested = 20m, decimal destinationQuantity = 0m)
            {
                var source = CreateWarehouse("W-1", "Main");
                var destination = CreateWarehouse("W-2", "Branch");
                var item = CreateItem();

                SourceInventory = CreateInventory(item, source, sourceQuantity, sourceUnitCost);
                DestinationInventory = CreateInventory(item, destination, destinationQuantity, sourceUnitCost);

                Transfer = new StockTransferOrder("ST-1", source, destination, Today, "IRR");
                Transfer.AddItem(new StockTransferOrderItem(Transfer, item, 1, requested, Unit, Money.Create(sourceUnitCost, "IRR")));

                Transfers.Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Transfer);

                Inventories.Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), source.Id, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(SourceInventory);

                Inventories.Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), destination.Id, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(DestinationInventory);

                Ledger.Setup(r => r.CreateAsync(It.IsAny<StockLedgerEntry>(), It.IsAny<CancellationToken>()))
                    .Callback<StockLedgerEntry, CancellationToken>((entry, _) => Entries.Add(entry))
                    .Returns(Task.CompletedTask);
            }

            public DispatchStockTransferCommandHandler Dispatcher() => new(
                Transfers.Object, Inventories.Object, Ledger.Object, new FixedClock(Now));

            public ReceiveStockTransferCommandHandler Receiver() => new(
                Transfers.Object, Inventories.Object, Ledger.Object, new FixedClock(Now));
        }

        [Fact]
        public async Task Approving_a_transfer_moves_no_stock()
        {
            var harness = new MoveHarness();
            var handler = new ApproveStockTransferCommandHandler(harness.Transfers.Object);

            var result = await handler.Handle(new ApproveStockTransferCommand { TransferId = harness.Transfer.Id }, Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(StockTransferStatus.Approved, result.Value.Status);
            Assert.Equal(100m, harness.SourceInventory.OnHandQuantity);
            Assert.Empty(harness.Entries);
        }

        [Fact]
        public async Task Dispatching_takes_the_stock_off_the_source_and_puts_it_nowhere()
        {
            var harness = new MoveHarness();
            harness.Transfer.Approve();

            var result = await harness.Dispatcher().Handle(
                new DispatchStockTransferCommand { TransferId = harness.Transfer.Id, DispatchDate = Today },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(StockTransferStatus.InTransit, result.Value.Status);

            // Off the source, and not yet at the destination: this is the balance nobody can
            // reconcile by walking around a building.
            Assert.Equal(80m, harness.SourceInventory.OnHandQuantity);
            Assert.Equal(0m, harness.DestinationInventory.OnHandQuantity);

            var entry = Assert.Single(harness.Entries);
            Assert.Equal(StockMovementType.TransferOut, entry.MovementType);
            Assert.Equal(-20m, entry.QuantityChange);
        }

        [Fact]
        public async Task A_transfer_that_was_not_approved_cannot_be_dispatched()
        {
            var harness = new MoveHarness();

            var result = await harness.Dispatcher().Handle(
                new DispatchStockTransferCommand { TransferId = harness.Transfer.Id, DispatchDate = Today },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("TransferCannotBeChanged", result.Error.Code);
            Assert.Equal(100m, harness.SourceInventory.OnHandQuantity);
        }

        [Fact]
        public async Task The_destination_books_in_the_same_value_the_source_booked_out()
        {
            // The whole point of a transfer. If the two sides could differ, moving stock between
            // two of your own buildings would create or destroy value out of nothing.
            var harness = new MoveHarness(sourceQuantity: 100m, sourceUnitCost: 250m, requested: 20m);
            harness.Transfer.Approve();

            await harness.Dispatcher().Handle(
                new DispatchStockTransferCommand { TransferId = harness.Transfer.Id, DispatchDate = Today }, Token);

            var outValue = harness.Entries.Single().ValueChange.Amount;

            var result = await harness.Receiver().Handle(
                new ReceiveStockTransferCommand { TransferId = harness.Transfer.Id, ReceiveDate = Today },
                Token);

            Assert.True(result.IsSuccess);

            var inValue = harness.Entries.Last().ValueChange.Amount;
            Assert.Equal(-outValue, inValue);
            Assert.Equal(20m, harness.DestinationInventory.OnHandQuantity);
            Assert.Equal(5_000m, harness.DestinationInventory.TotalValue.Amount);
        }

        [Fact]
        public async Task A_shortage_needs_a_reason_before_anything_is_written()
        {
            var harness = new MoveHarness();
            harness.Transfer.Approve();

            await harness.Dispatcher().Handle(
                new DispatchStockTransferCommand { TransferId = harness.Transfer.Id, DispatchDate = Today }, Token);

            var before = harness.Entries.Count;

            var result = await harness.Receiver().Handle(
                new ReceiveStockTransferCommand
                {
                    TransferId = harness.Transfer.Id,
                    ReceiveDate = Today,
                    Lines = [new StockTransferMovementLineDto { TransferItemId = harness.Transfer.Items[0].Id, Quantity = 15m }]
                },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("TransferCannotBeChanged", result.Error.Code);
            Assert.Equal(before, harness.Entries.Count);
        }

        [Fact]
        public async Task An_explained_shortage_closes_the_transfer_with_a_discrepancy()
        {
            var harness = new MoveHarness();
            harness.Transfer.Approve();

            await harness.Dispatcher().Handle(
                new DispatchStockTransferCommand { TransferId = harness.Transfer.Id, DispatchDate = Today }, Token);

            var result = await harness.Receiver().Handle(
                new ReceiveStockTransferCommand
                {
                    TransferId = harness.Transfer.Id,
                    ReceiveDate = Today,
                    Lines =
                    [
                        new StockTransferMovementLineDto
                        {
                            TransferItemId = harness.Transfer.Items[0].Id,
                            Quantity = 15m,
                            DiscrepancyReason = "Five boxes damaged in transit."
                        }
                    ]
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(StockTransferStatus.ReceivedWithDiscrepancy, result.Value.Status);
            Assert.Equal(5m, result.Value.Lines[0].ShortageQuantity);

            // Only what arrived becomes stock. The missing five are deliberately not written off
            // here: whether that is theft, damage or a miscount is somebody's decision.
            Assert.Equal(15m, harness.DestinationInventory.OnHandQuantity);
        }

        [Fact]
        public async Task Goods_cannot_arrive_before_they_left()
        {
            var harness = new MoveHarness();
            harness.Transfer.Approve();

            await harness.Dispatcher().Handle(
                new DispatchStockTransferCommand { TransferId = harness.Transfer.Id, DispatchDate = Today }, Token);

            var result = await harness.Receiver().Handle(
                new ReceiveStockTransferCommand { TransferId = harness.Transfer.Id, ReceiveDate = Today.AddDays(-1) },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("TransferCannotBeChanged", result.Error.Code);
        }

        private sealed class CountHarness
        {
            public Mock<IWarehouseRepository> Warehouses { get; } = new();
            public Mock<IItemInventoryRepository> Inventories { get; } = new();
            public Mock<IStockCountRepository> Counts { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public Mock<IStockLedgerEntryRepository> Ledger { get; } = new();
            public List<StockLedgerEntry> Entries { get; } = [];

            public Warehouse Warehouse { get; }
            public ItemInventory Inventory { get; }
            public StockCount? Created { get; private set; }

            public CountHarness(decimal onHand = 100m, decimal unitCost = 250m, bool itemHasUnit = true, IReadOnlyList<StockCount>? open = null)
            {
                Warehouse = CreateWarehouse("W-1", "Main");
                Inventory = CreateInventory(CreateItem(withUnit: itemHasUnit), Warehouse, onHand, unitCost);

                Warehouses.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Warehouse);

                Inventories.Setup(r => r.GetByWarehouseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync([Inventory]);

                Inventories.Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Inventory);

                Counts.Setup(r => r.GetOpenAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(open ?? []);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("StockCount", paddingLength: 5));

                Counts.Setup(r => r.CreateAsync(It.IsAny<StockCount>(), It.IsAny<CancellationToken>()))
                    .Callback<StockCount, CancellationToken>((count, _) => Created = count)
                    .Returns(Task.CompletedTask);

                Ledger.Setup(r => r.CreateAsync(It.IsAny<StockLedgerEntry>(), It.IsAny<CancellationToken>()))
                    .Callback<StockLedgerEntry, CancellationToken>((entry, _) => Entries.Add(entry))
                    .Returns(Task.CompletedTask);
            }

            public CreateStockCountCommandHandler Creator() => new(
                Warehouses.Object, Inventories.Object, Counts.Object, Series.Object);

            public RecordStockCountCommandHandler Recorder(StockCount count)
            {
                Counts.Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(count);
                return new RecordStockCountCommandHandler(Counts.Object);
            }

            public ApproveStockCountCommandHandler Approver(StockCount count)
            {
                Counts.Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(count);
                return new ApproveStockCountCommandHandler(Counts.Object, Inventories.Object, Ledger.Object, new FixedClock(Now));
            }
        }

        private static CreateStockCountCommand CountCommand(Guid warehouseId) => new()
        {
            WarehouseId = warehouseId,
            Type = StockCountType.Full,
            CountDate = Today
        };

        [Fact]
        public async Task A_sheet_is_built_and_frozen_together_so_nothing_moves_under_it()
        {
            var harness = new CountHarness(onHand: 100m);

            var result = await harness.Creator().Handle(CountCommand(harness.Warehouse.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(StockCountStatus.InProgress, result.Value.Status);
            Assert.NotNull(result.Value.FrozenOn);
            Assert.Equal(100m, result.Value.Lines[0].SystemQuantity);
        }

        [Fact]
        public async Task A_second_count_over_the_same_shelves_is_refused()
        {
            // Two open sheets produce two sets of adjustments for the same stock, and whichever
            // is approved second silently undoes the first.
            var existing = new StockCount("SC-9", Guid.NewGuid(), StockCountType.Full, Today);
            var harness = new CountHarness(open: [existing]);

            var result = await harness.Creator().Handle(CountCommand(harness.Warehouse.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("CountIsAlreadyOpen", result.Error.Code);
        }

        [Fact]
        public async Task An_item_with_no_base_unit_stops_the_sheet_rather_than_being_skipped()
        {
            // Skipping it would mean nobody ever counts that item, and nothing would ever say so.
            var harness = new CountHarness(itemHasUnit: false);

            var result = await harness.Creator().Handle(CountCommand(harness.Warehouse.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("ItemHasNoUnitOfMeasure", result.Error.Code);
        }

        [Fact]
        public async Task Recording_a_count_changes_nothing_about_the_books()
        {
            var harness = new CountHarness(onHand: 100m);
            var created = await harness.Creator().Handle(CountCommand(harness.Warehouse.Id), Token);
            var count = harness.Created!;

            var result = await harness.Recorder(count).Handle(
                new RecordStockCountCommand
                {
                    StockCountId = count.Id,
                    Lines = [new StockCountLineDto { StockCountItemId = created.Value.Lines[0].Id, CountedQuantity = 93m }]
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(-7m, result.Value.Lines[0].Variance);
            Assert.Equal(100m, harness.Inventory.OnHandQuantity);
            Assert.Empty(harness.Entries);
        }

        [Fact]
        public async Task A_sheet_is_submitted_only_once_every_line_has_been_counted()
        {
            var harness = new CountHarness(onHand: 100m);
            var created = await harness.Creator().Handle(CountCommand(harness.Warehouse.Id), Token);
            var count = harness.Created!;

            var result = await harness.Recorder(count).Handle(
                new RecordStockCountCommand
                {
                    StockCountId = count.Id,
                    Lines = [new StockCountLineDto { StockCountItemId = created.Value.Lines[0].Id, CountedQuantity = 93m }],
                    SubmitWhenComplete = true
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(StockCountStatus.AwaitingApproval, result.Value.Status);
        }

        [Fact]
        public async Task Approving_a_count_sets_the_books_to_what_was_found()
        {
            var harness = new CountHarness(onHand: 100m, unitCost: 250m);
            var created = await harness.Creator().Handle(CountCommand(harness.Warehouse.Id), Token);
            var count = harness.Created!;

            await harness.Recorder(count).Handle(
                new RecordStockCountCommand
                {
                    StockCountId = count.Id,
                    Lines = [new StockCountLineDto { StockCountItemId = created.Value.Lines[0].Id, CountedQuantity = 93m, DiscrepancyReason = "Breakage." }],
                    SubmitWhenComplete = true
                },
                Token);

            var result = await harness.Approver(count).Handle(
                new ApproveStockCountCommand { StockCountId = count.Id, ApprovedByHumanId = Guid.NewGuid() },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(StockCountStatus.Completed, result.Value.Status);
            Assert.Equal(93m, harness.Inventory.OnHandQuantity);

            var entry = Assert.Single(harness.Entries);
            Assert.Equal(StockMovementType.CountAdjustment, entry.MovementType);
            Assert.Equal(-7m, entry.QuantityChange);

            // The ledger records the value the inventory actually lost, so the card and the
            // balance can never drift apart over an adjustment.
            Assert.Equal(-1_750m, entry.ValueChange.Amount);
        }

        [Fact]
        public async Task A_count_nobody_submitted_cannot_be_approved()
        {
            var harness = new CountHarness(onHand: 100m);
            await harness.Creator().Handle(CountCommand(harness.Warehouse.Id), Token);
            var count = harness.Created!;

            var result = await harness.Approver(count).Handle(
                new ApproveStockCountCommand { StockCountId = count.Id, ApprovedByHumanId = Guid.NewGuid() },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("CountCannotBeChanged", result.Error.Code);
            Assert.Equal(100m, harness.Inventory.OnHandQuantity);
        }

        [Fact]
        public async Task A_line_that_matched_the_books_produces_no_adjustment()
        {
            var harness = new CountHarness(onHand: 100m);
            var created = await harness.Creator().Handle(CountCommand(harness.Warehouse.Id), Token);
            var count = harness.Created!;

            await harness.Recorder(count).Handle(
                new RecordStockCountCommand
                {
                    StockCountId = count.Id,
                    Lines = [new StockCountLineDto { StockCountItemId = created.Value.Lines[0].Id, CountedQuantity = 100m }],
                    SubmitWhenComplete = true
                },
                Token);

            var result = await harness.Approver(count).Handle(
                new ApproveStockCountCommand { StockCountId = count.Id, ApprovedByHumanId = Guid.NewGuid() },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Empty(harness.Entries);
        }

        private sealed class CardHarness
        {
            public Mock<IItemRepository> Items { get; } = new();
            public Mock<IWarehouseRepository> Warehouses { get; } = new();
            public Mock<IStockLedgerEntryRepository> Ledger { get; } = new();

            public Item Item { get; }
            public Warehouse Warehouse { get; }

            public CardHarness((decimal Quantity, decimal Value) opening, IReadOnlyList<StockLedgerEntry> movements)
            {
                Item = CreateItem();
                Warehouse = CreateWarehouse("W-1", "Main");

                Items.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Item);
                Warehouses.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Warehouse);

                Ledger.Setup(r => r.GetBalanceAtAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(opening);

                Ledger.Setup(r => r.GetCardAsync(
                        It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(movements);
            }

            public GetItemStockCardQueryHandler Build() => new(
                Items.Object, Warehouses.Object, Ledger.Object, new FixedClock(Now));
        }

        private StockLedgerEntry Movement(Item item, Warehouse warehouse, decimal quantity, decimal value, decimal balanceQuantity, decimal balanceValue) =>
            new(item, warehouse.Id, quantity > 0m ? StockMovementType.PurchaseReceipt : StockMovementType.SalesIssue,
                quantity, Money.Create(value, "IRR"), Unit, balanceQuantity, Money.Create(balanceValue, "IRR"), Now);

        [Fact]
        public async Task The_card_opens_and_closes_on_the_balances_either_side_of_the_window()
        {
            var item = CreateItem();
            var warehouse = CreateWarehouse("W-1", "Main");

            var movements = new[]
            {
                Movement(item, warehouse, 30m, 7_500m, 80m, 20_000m),
                Movement(item, warehouse, -10m, -2_500m, 70m, 17_500m)
            };

            var harness = new CardHarness((50m, 12_500m), movements);

            var result = await harness.Build().Handle(
                new GetItemStockCardQuery { ItemId = item.Id, WarehouseId = warehouse.Id, From = Today.AddDays(-7), To = Today },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(50m, result.Value.OpeningQuantity);
            Assert.Equal(30m, result.Value.TotalIn);
            Assert.Equal(10m, result.Value.TotalOut);
            Assert.Equal(70m, result.Value.ClosingQuantity);
            Assert.Equal(17_500m, result.Value.ClosingValue);
        }

        [Fact]
        public async Task A_window_with_no_movements_closes_where_it_opened()
        {
            var item = CreateItem();
            var warehouse = CreateWarehouse("W-1", "Main");
            var harness = new CardHarness((50m, 12_500m), []);

            var result = await harness.Build().Handle(
                new GetItemStockCardQuery { ItemId = item.Id, WarehouseId = warehouse.Id },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(50m, result.Value.ClosingQuantity);
            Assert.Equal(12_500m, result.Value.ClosingValue);
            Assert.Empty(result.Value.Movements);
        }

        [Fact]
        public async Task Each_movement_is_reported_on_the_side_it_belongs_to()
        {
            var item = CreateItem();
            var warehouse = CreateWarehouse("W-1", "Main");
            var harness = new CardHarness((0m, 0m), [Movement(item, warehouse, -10m, -2_500m, 40m, 10_000m)]);

            var result = await harness.Build().Handle(
                new GetItemStockCardQuery { ItemId = item.Id, WarehouseId = warehouse.Id },
                Token);

            Assert.True(result.IsSuccess);

            var line = Assert.Single(result.Value.Movements);
            Assert.Equal(0m, line.QuantityIn);
            Assert.Equal(10m, line.QuantityOut);
        }
    }
}
