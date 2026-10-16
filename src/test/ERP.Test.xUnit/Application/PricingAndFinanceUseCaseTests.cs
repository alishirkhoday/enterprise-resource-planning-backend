using CSharpFunctionalExtensions;
using ERP.Application.Common;
using ERP.Application.UseCases.Modules.FinanceManagement.Commands.RunDepreciation;
using ERP.Application.UseCases.Modules.FinanceManagement.Commands.SettleAccount;
using ERP.Application.UseCases.Modules.FinanceManagement.DTOs;
using ERP.Application.UseCases.Modules.FinanceManagement.Queries.CheckBudget;
using ERP.Application.UseCases.Modules.PricingManagement.Commands.CreatePriceList;
using ERP.Application.UseCases.Modules.PricingManagement.Commands.SetPriceListItems;
using ERP.Application.UseCases.Modules.PricingManagement.DTOs;
using ERP.Application.UseCases.Modules.PricingManagement.Queries.GetEffectivePrice;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.PricingManagement;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.PricingManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// Price lists, depreciation, budgets and settlement.
    /// <para>
    /// All four are places where a number quietly becomes wrong and stays wrong: a price nobody
    /// checked, an asset left out of a run, a budget that looks healthy until the invoices land,
    /// and cash applied to the wrong debt.
    /// </para>
    /// </summary>
    public sealed class PricingAndFinanceUseCaseTests
    {
        private static readonly Guid Unit = Guid.NewGuid();
        private static readonly DateOnly Today = new(2026, 5, 10);
        private static readonly DateTimeOffset Now = new(2026, 5, 10, 9, 0, 0, TimeSpan.Zero);

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private sealed class FixedClock(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private static Item CreateItem(string name = "Bolt")
        {
            var item = new Item(new ItemGroup("G-1", "Fasteners"), ItemType.Part);
            item.Update("SKU-1", name);
            item.SetUsage(isPurchasable: true, isSellable: true, isManufactured: false);
            return item;
        }

        private static PriceList CreatePriceList(string code = "PL-1") =>
            new(code, "Retail", PriceListType.Sales, "IRR",
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));

        private sealed class PriceListHarness
        {
            public Mock<IPriceListRepository> PriceLists { get; } = new();
            public Mock<IItemRepository> Items { get; } = new();
            public PriceList? Created { get; private set; }

            public PriceListHarness(Item item, PriceList? duplicate = null, PriceList? existingDefault = null)
            {
                PriceLists.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(duplicate);

                PriceLists.Setup(r => r.GetActiveOnAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existingDefault is null ? [] : [existingDefault]);

                Items.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([item]);

                PriceLists.Setup(r => r.CreateAsync(It.IsAny<PriceList>(), It.IsAny<CancellationToken>()))
                    .Callback<PriceList, CancellationToken>((list, _) => Created = list)
                    .Returns(Task.CompletedTask);
            }

            public CreatePriceListCommandHandler Build() => new(PriceLists.Object, Items.Object);
        }

        private static CreatePriceListCommand PriceListCommand(Guid itemId, bool isDefault = false, decimal price = 500m) => new()
        {
            Code = "PL-1",
            Name = "Retail",
            Type = PriceListType.Sales,
            CurrencyCode = "IRR",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            IsDefault = isDefault,
            Lines = [new PriceListLineDto { ItemId = itemId, UnitOfMeasureId = Unit, BasePrice = price }]
        };

        [Fact]
        public async Task A_price_list_carries_the_lines_it_was_created_with()
        {
            var item = CreateItem();
            var harness = new PriceListHarness(item);

            var result = await harness.Build().Handle(PriceListCommand(item.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.ItemCount);
            Assert.Equal(500m, result.Value.Items[0].BasePrice);
        }

        [Fact]
        public async Task A_duplicate_code_is_refused()
        {
            var item = CreateItem();
            var harness = new PriceListHarness(item, duplicate: CreatePriceList());

            var result = await harness.Build().Handle(PriceListCommand(item.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("PriceListCodeIsUsed", result.Error.Code);
        }

        [Fact]
        public async Task Making_a_list_the_default_takes_that_position_off_the_one_that_held_it()
        {
            // Two defaults means the resolution picks whichever row came back first, so a
            // customer with no named list gets a different price depending on the query plan.
            var item = CreateItem();
            var incumbent = CreatePriceList("PL-OLD");
            incumbent.MarkAsDefault();

            var harness = new PriceListHarness(item, existingDefault: incumbent);

            var result = await harness.Build().Handle(PriceListCommand(item.Id, isDefault: true), Token);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value.IsDefault);
            Assert.False(incumbent.IsDefault);
        }

        [Fact]
        public async Task Setting_prices_restates_an_existing_break_rather_than_duplicating_it()
        {
            var item = CreateItem();
            var priceList = CreatePriceList();
            priceList.AddItem(new PriceListItem(priceList, item, Unit, Money.Create(500m, "IRR")));

            var priceLists = new Mock<IPriceListRepository>();
            priceLists.Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(priceList);

            var items = new Mock<IItemRepository>();
            items.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([item]);

            var result = await new SetPriceListItemsCommandHandler(priceLists.Object, items.Object).Handle(
                new SetPriceListItemsCommand
                {
                    PriceListId = priceList.Id,
                    Lines = [new PriceListLineDto { ItemId = item.Id, UnitOfMeasureId = Unit, BasePrice = 650m }]
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.ItemCount);
            Assert.Equal(650m, result.Value.Items[0].BasePrice);
        }

        [Fact]
        public async Task Replacing_a_whole_list_drops_what_is_no_longer_priced()
        {
            var kept = CreateItem("Bolt");
            var dropped = CreateItem("Washer");

            var priceList = CreatePriceList();
            priceList.AddItem(new PriceListItem(priceList, kept, Unit, Money.Create(500m, "IRR")));
            priceList.AddItem(new PriceListItem(priceList, dropped, Unit, Money.Create(100m, "IRR")));

            var priceLists = new Mock<IPriceListRepository>();
            priceLists.Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(priceList);

            var items = new Mock<IItemRepository>();
            items.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([kept]);

            var result = await new SetPriceListItemsCommandHandler(priceLists.Object, items.Object).Handle(
                new SetPriceListItemsCommand
                {
                    PriceListId = priceList.Id,
                    ReplaceAll = true,
                    Lines = [new PriceListLineDto { ItemId = kept.Id, UnitOfMeasureId = Unit, BasePrice = 550m }]
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.ItemCount);
            Assert.Equal(kept.Id, result.Value.Items[0].ItemId);
        }

        [Fact]
        public async Task A_price_quote_takes_the_highest_volume_break_the_quantity_qualifies_for()
        {
            var item = CreateItem();
            var priceList = CreatePriceList();
            priceList.AddItem(new PriceListItem(priceList, item, Unit, Money.Create(500m, "IRR")));
            priceList.AddItem(new PriceListItem(priceList, item, Unit, Money.Create(420m, "IRR"), minimumQuantity: 100m));

            var items = new Mock<IItemRepository>();
            items.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(item);

            var priceLists = new Mock<IPriceListRepository>();
            priceLists.Setup(r => r.GetEffectiveForSaleAsync(
                    It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(priceList);

            var handler = new GetEffectivePriceQueryHandler(
                items.Object, new Mock<ICustomerRepository>().Object, priceLists.Object, new FixedClock(Now));

            var result = await handler.Handle(
                new GetEffectivePriceQuery { ItemId = item.Id, Quantity = 150m, CurrencyCode = "IRR" },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(420m, result.Value.NetPrice);
            Assert.Equal(63_000m, result.Value.LineTotal);
        }

        [Fact]
        public async Task An_item_with_no_price_is_refused_rather_than_quoted_at_nothing()
        {
            var item = CreateItem();

            var items = new Mock<IItemRepository>();
            items.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(item);

            var priceLists = new Mock<IPriceListRepository>();
            priceLists.Setup(r => r.GetEffectiveForSaleAsync(
                    It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreatePriceList());

            var handler = new GetEffectivePriceQueryHandler(
                items.Object, new Mock<ICustomerRepository>().Object, priceLists.Object, new FixedClock(Now));

            var result = await handler.Handle(new GetEffectivePriceQuery { ItemId = item.Id }, Token);

            Assert.True(result.IsFailure);
            Assert.Equal("NoPriceForItem", result.Error.Code);
        }

        private static FixedAsset CreateAsset(decimal cost = 120_000_000m, int usefulLifeMonths = 60, bool inService = true)
        {
            var category = new AssetCategory("AC-1", "Machinery", DepreciationMethod.StraightLine, 60);
            var asset = new FixedAsset(category, "FA-1", "Lathe", new DateOnly(2026, 1, 1), Money.Create(cost, "IRR"));

            asset.SetDepreciationPolicy(DepreciationMethod.StraightLine, usefulLifeMonths, Money.Zero("IRR"));

            if (inService)
                asset.PlaceInService(new DateOnly(2026, 1, 1));

            return asset;
        }

        private sealed class DepreciationHarness
        {
            public Mock<IFixedAssetRepository> Assets { get; } = new();

            public DepreciationHarness(params FixedAsset[] assets)
            {
                Assets.Setup(r => r.GetDueForDepreciationAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(assets);

                Assets.Setup(r => r.HasDepreciationForPeriodAsync(
                        It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
            }

            public RunDepreciationCommandHandler Build() => new(Assets.Object);
        }

        private static RunDepreciationCommand DepreciationCommand(bool previewOnly = false) => new()
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 31),
            PreviewOnly = previewOnly
        };

        [Fact]
        public async Task A_run_charges_every_asset_in_service()
        {
            var asset = CreateAsset(cost: 120_000_000m, usefulLifeMonths: 60);
            var harness = new DepreciationHarness(asset);

            var result = await harness.Build().Handle(DepreciationCommand(), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.AssetCount);
            Assert.Equal(2_000_000m, result.Value.TotalCharge);
            Assert.Equal(2_000_000m, asset.AccumulatedDepreciation.Amount);
        }

        [Fact]
        public async Task A_preview_reports_the_charge_and_writes_nothing()
        {
            // What a controller does before committing to a month end.
            var asset = CreateAsset();
            var harness = new DepreciationHarness(asset);

            var result = await harness.Build().Handle(DepreciationCommand(previewOnly: true), Token);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value.WasPreview);
            Assert.Equal(2_000_000m, result.Value.TotalCharge);
            Assert.Equal(0m, asset.AccumulatedDepreciation.Amount);
        }

        [Fact]
        public async Task An_asset_already_charged_for_the_period_is_reported_rather_than_charged_twice()
        {
            var asset = CreateAsset();
            var harness = new DepreciationHarness(asset);

            harness.Assets.Setup(r => r.HasDepreciationForPeriodAsync(
                    It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await harness.Build().Handle(DepreciationCommand(), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value.AssetCount);
            Assert.Equal(1, result.Value.SkippedCount);
            Assert.Equal(0m, asset.AccumulatedDepreciation.Amount);
        }

        [Fact]
        public async Task An_asset_carried_in_another_currency_is_skipped_with_a_reason()
        {
            // Adding it into the total would produce a figure that means nothing. Reporting it is
            // what lets somebody run those assets separately rather than never noticing.
            var inRials = CreateAsset();

            var euroCategory = new AssetCategory("AC-2", "Imported", DepreciationMethod.StraightLine, 60);
            var inEuros = new FixedAsset(euroCategory, "FA-2", "Press", new DateOnly(2026, 1, 1), Money.Create(50_000m, "EUR"));
            inEuros.SetDepreciationPolicy(DepreciationMethod.StraightLine, 60, Money.Zero("EUR"));
            inEuros.PlaceInService(new DateOnly(2026, 1, 1));

            var harness = new DepreciationHarness(inRials, inEuros);

            var result = await harness.Build().Handle(DepreciationCommand(), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.AssetCount);
            Assert.Equal(1, result.Value.SkippedCount);
            Assert.Contains("EUR", result.Value.Skipped[0].Reason);
        }

        [Fact]
        public async Task A_run_with_nothing_in_service_says_so()
        {
            var harness = new DepreciationHarness();

            var result = await harness.Build().Handle(DepreciationCommand(), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("NothingToDepreciate", result.Error.Code);
        }

        private static BudgetLine CreateBudgetLine(decimal planned, decimal actual = 0m, decimal committed = 0m)
        {
            var account = new Account("6101", "Stationery", AccountType.ExpenseOperating, AccountGroup.LevelFour, AccountBalance.Debtor, FinancialStatement.IncomeAndExpenseStatement, isFinal: true);
            var budget = new Budget(Guid.NewGuid(), "2026 operating", BudgetType.Operating, "IRR");
            var line = new BudgetLine(budget, account, 5, Money.Create(planned, "IRR"));

            if (actual > 0m)
                line.RecordActual(Money.Create(actual, "IRR"));

            if (committed > 0m)
                line.Commit(Money.Create(committed, "IRR"));

            return line;
        }

        private static async Task<Result<BudgetCheckDto, Error>> CheckAsync(BudgetLine? line, decimal amount)
        {
            var budgets = new Mock<IBudgetRepository>();
            budgets.Setup(r => r.GetLineForAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<short>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(line);

            return await new CheckBudgetQueryHandler(budgets.Object).Handle(
                new CheckBudgetQuery
                {
                    FinancialYearId = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    PeriodNumber = 5,
                    Amount = amount
                },
                Token);
        }

        [Fact]
        public async Task Money_already_committed_counts_against_the_budget_before_the_invoice_arrives()
        {
            // Without this a budget looks healthy right up to the month the invoices land.
            var line = CreateBudgetLine(planned: 10_000_000m, actual: 3_000_000m, committed: 6_000_000m);

            var result = await CheckAsync(line, 2_000_000m);

            Assert.True(result.IsSuccess);
            Assert.Equal(1_000_000m, result.Value.Available);
            Assert.False(result.Value.Fits);
        }

        [Fact]
        public async Task A_spend_inside_what_is_left_fits()
        {
            var line = CreateBudgetLine(planned: 10_000_000m, actual: 3_000_000m);

            var result = await CheckAsync(line, 2_000_000m);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value.Fits);
            Assert.Equal(5_000_000m, result.Value.WouldBe);
        }

        [Fact]
        public async Task An_account_nobody_budgeted_is_a_refusal_rather_than_an_unlimited_pass()
        {
            var result = await CheckAsync(null, 2_000_000m);

            Assert.True(result.IsFailure);
            Assert.Equal("BudgetLineNotFound", result.Error.Code);
        }

        private sealed class SettlementHarness
        {
            public Mock<ISettlementRepository> Settlements { get; } = new();
            public Mock<IReceivableRepository> Receivables { get; } = new();
            public Mock<IPayableRepository> Payables { get; } = new();
            public Settlement? Created { get; private set; }

            public SettlementHarness(Receivable? receivable = null, Payable? payable = null)
            {
                Receivables.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(receivable);
                Payables.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(payable);

                Settlements.Setup(r => r.CreateAsync(It.IsAny<Settlement>(), It.IsAny<CancellationToken>()))
                    .Callback<Settlement, CancellationToken>((settlement, _) => Created = settlement)
                    .Returns(Task.CompletedTask);
            }

            public SettleAccountCommandHandler Build() => new(Settlements.Object, Receivables.Object, Payables.Object);
        }

        private static Receivable CreateReceivable(decimal amount = 10_000_000m) =>
            new(Guid.NewGuid(), Money.Create(amount, "IRR"), Today.AddDays(-30), Today);

        private static SettleAccountCommand SettleCommand(decimal amount, Guid? receivableId = null, decimal? allocated = null, decimal? discount = null) => new()
        {
            Direction = SettlementDirection.Receipt,
            Method = SettlementMethod.BankTransfer,
            BankBranchAccountId = Guid.NewGuid(),
            PartyDetailAccountId = Guid.NewGuid(),
            Amount = amount,
            CurrencyCode = "IRR",
            SettlementDate = Today,
            Allocations = receivableId is null
                ? []
                : [new SettlementAllocationDto { ReceivableId = receivableId, Amount = allocated ?? amount, DiscountTaken = discount }]
        };

        [Fact]
        public async Task Settling_an_invoice_moves_the_receivable_in_the_same_step()
        {
            // Split apart, cash sits unapplied and a customer who has paid still shows as overdue.
            var receivable = CreateReceivable(10_000_000m);
            var harness = new SettlementHarness(receivable);

            var result = await harness.Build().Handle(SettleCommand(10_000_000m, receivable.Id), Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(ReceivableStatus.Settled, receivable.Status);
            Assert.Equal(0m, receivable.OutstandingAmount.Amount);
            Assert.Equal(0m, result.Value.UnallocatedAmount);
        }

        [Fact]
        public async Task Applying_more_than_was_received_is_refused_before_anything_is_written()
        {
            var receivable = CreateReceivable();
            var harness = new SettlementHarness(receivable);

            var result = await harness.Build().Handle(SettleCommand(5_000_000m, receivable.Id, allocated: 8_000_000m), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("OverAllocated", result.Error.Code);
            Assert.Null(harness.Created);
            Assert.Equal(ReceivableStatus.Open, receivable.Status);
        }

        [Fact]
        public async Task An_early_settlement_discount_closes_the_invoice_rather_than_leaving_a_remainder()
        {
            var receivable = CreateReceivable(10_000_000m);
            var harness = new SettlementHarness(receivable);

            var result = await harness.Build().Handle(
                SettleCommand(9_500_000m, receivable.Id, allocated: 9_500_000m, discount: 500_000m),
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(ReceivableStatus.Settled, receivable.Status);
        }

        [Fact]
        public async Task Money_taken_as_an_advance_stays_on_account()
        {
            // A customer paying before being invoiced. It has to be recorded as an advance: cash
            // with no allocation and no explanation is what somebody writes off a year later.
            var harness = new SettlementHarness();

            var result = await harness.Build().Handle(
                SettleCommand(5_000_000m) with { Method = SettlementMethod.Advance },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(5_000_000m, result.Value.UnallocatedAmount);
            Assert.Equal(SettlementStatus.Confirmed, result.Value.Status);
        }

        [Fact]
        public async Task Cash_with_nothing_to_apply_it_to_is_refused_unless_it_is_an_advance()
        {
            var harness = new SettlementHarness();

            var result = await harness.Build().Handle(SettleCommand(5_000_000m), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("SettlementCannotBeChanged", result.Error.Code);
        }

        [Fact]
        public async Task A_receivable_already_settled_cannot_take_more_money()
        {
            var receivable = CreateReceivable(10_000_000m);
            receivable.Settle(Money.Create(10_000_000m, "IRR"), Today);

            var harness = new SettlementHarness(receivable);

            var result = await harness.Build().Handle(SettleCommand(1_000_000m, receivable.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("ItemIsNotOpen", result.Error.Code);
        }
    }
}
