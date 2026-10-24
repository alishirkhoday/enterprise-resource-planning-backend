using ERP.Application.UseCases.Modules.PlanningManagement.Commands.ReleasePlannedOrder;
using ERP.Application.UseCases.Modules.PlanningManagement.Commands.RunMaterialRequirementPlan;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.PlanningManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.PlanningManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// Planning proposes; it never commits. These hold it to that, and to not letting one shortage
    /// become two orders.
    /// </summary>
    public sealed class PlanningUseCaseTests
    {
        private static readonly Guid Unit = Guid.NewGuid();
        private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private sealed class FixedClock(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private static Item CreateItem(bool isManufactured = false)
        {
            var item = new Item(new ItemGroup("G-1", "Pumps"), ItemType.Product);
            item.Update("SKU-1", "Pump");
            item.SetUsage(isPurchasable: !isManufactured, isSellable: true, isManufactured: isManufactured);
            item.SetBaseUnitOfMeasure(Unit);
            return item;
        }

        private static DemandForecast CreateForecast(Item item, decimal quantity, bool approve = true)
        {
            var forecast = new DemandForecast(
                "FC-1",
                "First half",
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)),
                ForecastBucket.Monthly,
                ForecastMethod.Manual);

            forecast.AddLine(new DemandForecastLine(
                forecast, item, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), quantity, Guid.NewGuid()));

            if (approve)
                forecast.Approve("planner");

            return forecast;
        }

        private sealed class RunHarness
        {
            public Mock<IDemandForecastRepository> Forecasts { get; } = new();
            public Mock<IItemRepository> Items { get; } = new();
            public Mock<IItemInventoryRepository> Inventories { get; } = new();
            public Mock<IPurchaseOrderRepository> PurchaseOrders { get; } = new();
            public Mock<IProductionOrderRepository> ProductionOrders { get; } = new();
            public Mock<IMaterialRequirementPlanRepository> Plans { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public MaterialRequirementPlan? Created { get; private set; }

            public RunHarness(Item item, DemandForecast? forecast, decimal onHand = 0m, decimal onOrder = 0m)
            {
                Forecasts.Setup(r => r.GetWithLinesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(forecast);
                Items.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([item]);

                Inventories.Setup(r => r.GetTotalAvailableAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(onHand);
                Inventories.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ItemInventory?)null);

                PurchaseOrders.Setup(r => r.GetQuantityOnOrderAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(onOrder);
                ProductionOrders.Setup(r => r.GetQuantityInProductionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(0m);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("MaterialRequirementPlan", paddingLength: 5));

                Plans.Setup(r => r.CreateAsync(It.IsAny<MaterialRequirementPlan>(), It.IsAny<CancellationToken>()))
                    .Callback<MaterialRequirementPlan, CancellationToken>((plan, _) => Created = plan)
                    .Returns(Task.CompletedTask);
            }

            public RunMaterialRequirementPlanCommandHandler Build() => new(
                Forecasts.Object, Items.Object, Inventories.Object, PurchaseOrders.Object,
                ProductionOrders.Object, Plans.Object, Series.Object, new FixedClock(Now));
        }

        private static RunMaterialRequirementPlanCommand RunCommand(Guid itemId, Guid? forecastId) => new()
        {
            HorizonStart = new DateOnly(2026, 3, 1),
            HorizonEnd = new DateOnly(2026, 6, 30),
            DemandForecastId = forecastId,
            ItemIds = [itemId]
        };

        /// <summary>
        /// Planning against a draft forecast commits money to numbers nobody has agreed yet.
        /// </summary>
        [Fact]
        public async Task A_draft_forecast_cannot_be_planned_against()
        {
            var item = CreateItem();
            var forecast = CreateForecast(item, 100m, approve: false);
            var harness = new RunHarness(item, forecast);

            var result = await harness.Build().Handle(RunCommand(item.Id, forecast.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("ForecastIsNotApproved", result.Error.Code);
        }

        [Fact]
        public async Task Demand_beyond_what_is_on_hand_and_on_order_becomes_a_shortage()
        {
            var item = CreateItem();
            var forecast = CreateForecast(item, 100m);
            var harness = new RunHarness(item, forecast, onHand: 30m, onOrder: 20m);

            var result = await harness.Build().Handle(RunCommand(item.Id, forecast.Id), Token);

            Assert.True(result.IsSuccess);
            var shortage = Assert.Single(result.Value.Shortages);
            Assert.Equal(-50m, shortage.ProjectedOnHand);
            Assert.Equal(50m, shortage.NetRequirement);
        }

        [Fact]
        public async Task Demand_already_covered_produces_no_shortage()
        {
            var item = CreateItem();
            var forecast = CreateForecast(item, 100m);
            var harness = new RunHarness(item, forecast, onHand: 120m);

            var result = await harness.Build().Handle(RunCommand(item.Id, forecast.Id), Token);

            Assert.Empty(result.Value.Shortages);
            Assert.Equal(1, result.Value.LineCount);
        }

        /// <summary>
        /// Made items become production orders and bought items become requisitions; the run has
        /// to say which, or the shortage is one nobody can act on.
        /// </summary>
        [Theory]
        [InlineData(false, PlannedOrderAction.Purchase)]
        [InlineData(true, PlannedOrderAction.Manufacture)]
        public async Task The_suggested_action_follows_how_the_item_is_replenished(bool isManufactured, PlannedOrderAction expected)
        {
            var item = CreateItem(isManufactured);
            var forecast = CreateForecast(item, 100m);
            var harness = new RunHarness(item, forecast);

            var result = await harness.Build().Handle(RunCommand(item.Id, forecast.Id), Token);

            Assert.Equal(expected, Assert.Single(result.Value.Shortages).SuggestedAction);
        }

        /// <summary>
        /// Nothing is committed by a run. Every line is a suggestion until somebody releases it.
        /// </summary>
        [Fact]
        public async Task A_run_releases_nothing_by_itself()
        {
            var item = CreateItem();
            var forecast = CreateForecast(item, 100m);
            var harness = new RunHarness(item, forecast);

            var result = await harness.Build().Handle(RunCommand(item.Id, forecast.Id), Token);

            Assert.All(result.Value.Shortages, line => Assert.False(line.IsReleased));
            Assert.Equal(MaterialRequirementPlanStatus.Completed, result.Value.Status);
        }

        private static MaterialRequirementPlan CreateCompletedPlan(Item item, PlannedOrderAction action = PlannedOrderAction.Purchase)
        {
            var plan = new MaterialRequirementPlan("MRP-1", DateRange.Create(new DateOnly(2026, 3, 1), new DateOnly(2026, 6, 30)));

            var line = new MaterialRequirementPlanLine(plan, item, new DateOnly(2026, 6, 30), 100m, 0m, 0m, Guid.NewGuid());
            plan.AddLine(line);
            line.Plan.SuggestMaterialRequirementPlanLine(line.Id, action, 100m, leadTimeDays: 14);
            plan.Complete();

            return plan;
        }

        private static Mock<IMaterialRequirementPlanRepository> PlansReturning(MaterialRequirementPlan plan)
        {
            var plans = new Mock<IMaterialRequirementPlanRepository>();
            plans.Setup(r => r.GetWithLinesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(plan);
            return plans;
        }

        /// <summary>
        /// The mark is what stops one shortage becoming two orders when the next person opens the
        /// plan.
        /// </summary>
        [Fact]
        public async Task A_released_line_cannot_be_released_again()
        {
            var plan = CreateCompletedPlan(CreateItem());
            var line = plan.Lines[0];
            var plans = PlansReturning(plan);
            var handler = new ReleasePlannedOrderCommandHandler(plans.Object);

            var first = await handler.Handle(new ReleasePlannedOrderCommand { PlanId = plan.Id, LineIds = [line.Id] }, Token);
            var second = await handler.Handle(new ReleasePlannedOrderCommand { PlanId = plan.Id, LineIds = [line.Id] }, Token);

            Assert.True(first.IsSuccess);
            Assert.True(second.IsFailure);
            Assert.Equal("LineIsAlreadyReleased", second.Error.Code);
        }

        [Theory]
        [InlineData(PlannedOrderAction.Purchase, "PurchaseRequisition")]
        [InlineData(PlannedOrderAction.Manufacture, "ProductionOrder")]
        public async Task A_release_records_what_the_suggestion_became(PlannedOrderAction action, string expectedReference)
        {
            var plan = CreateCompletedPlan(CreateItem(), action);
            var plans = PlansReturning(plan);

            var result = await new ReleasePlannedOrderCommandHandler(plans.Object)
                .Handle(new ReleasePlannedOrderCommand { PlanId = plan.Id, LineIds = [plan.Lines[0].Id] }, Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(expectedReference, Assert.Single(result.Value.Released).ReferenceType);
            Assert.True(plan.Lines[0].IsReleased);
        }

        /// <summary>
        /// A superseded run computed its suggestions against a position that has since moved.
        /// </summary>
        [Fact]
        public async Task A_superseded_plan_cannot_be_released_from()
        {
            var plan = CreateCompletedPlan(CreateItem());
            plan.Supersede();
            var plans = PlansReturning(plan);

            var result = await new ReleasePlannedOrderCommandHandler(plans.Object)
                .Handle(new ReleasePlannedOrderCommand { PlanId = plan.Id, LineIds = [plan.Lines[0].Id] }, Token);

            Assert.True(result.IsFailure);
            Assert.Equal("PlanIsNotActionable", result.Error.Code);
        }

        [Fact]
        public async Task A_line_from_another_plan_is_refused()
        {
            var plan = CreateCompletedPlan(CreateItem());
            var plans = PlansReturning(plan);

            var result = await new ReleasePlannedOrderCommandHandler(plans.Object)
                .Handle(new ReleasePlannedOrderCommand { PlanId = plan.Id, LineIds = [Guid.NewGuid()] }, Token);

            Assert.True(result.IsFailure);
            Assert.Equal("LineIsNotOnThePlan", result.Error.Code);
        }
    }
}
