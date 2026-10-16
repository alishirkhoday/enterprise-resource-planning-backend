using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.PlanningManagement;
using ERP.Domain.Entities.Modules.ProductionManagement;

namespace ERP.Test.xUnit.Domain
{
    /// <summary>
    /// Planning is arithmetic that somebody spends money on. These tests pin the arithmetic down,
    /// and the two rules that stop a suggestion being acted on twice or acted on late.
    /// </summary>
    public sealed class PlanningTests
    {
        private static readonly Guid Unit = Guid.NewGuid();

        private static Item CreateItem() => new(new ItemGroup("G-1", "Fasteners"), ItemType.Part);

        private static MaterialRequirementPlan CreateRunningPlan() =>
            new("MRP-1", DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)));

        private static MaterialRequirementPlanLine CreateLine(
            decimal gross = 100m,
            decimal scheduled = 0m,
            decimal opening = 0m,
            MaterialRequirementPlan? plan = null) =>
            new(plan ?? CreateRunningPlan(), CreateItem(), new DateOnly(2026, 3, 1), gross, scheduled, opening, Unit);

        [Fact]
        public void The_projection_is_opening_plus_supply_less_demand()
        {
            var line = CreateLine(gross: 100m, scheduled: 30m, opening: 25m);

            Assert.Equal(-45m, line.ProjectedOnHand);
        }

        /// <summary>
        /// Safety stock is not a buffer planning is allowed to eat. A projection that sits exactly
        /// on zero is still short by the whole safety quantity.
        /// </summary>
        [Fact]
        public void Safety_stock_is_not_available_to_plan_away()
        {
            var line = CreateLine(gross: 100m, scheduled: 0m, opening: 100m);
            line.SetSafetyQuantity(20m);

            Assert.Equal(0m, line.ProjectedOnHand);
            Assert.Equal(20m, line.NetRequirement);
        }

        [Fact]
        public void A_covered_requirement_is_short_of_nothing()
        {
            var line = CreateLine(gross: 40m, scheduled: 0m, opening: 100m);
            line.SetSafetyQuantity(20m);

            Assert.Equal(0m, line.NetRequirement);
            Assert.False(line.NeedsAction);
        }

        /// <summary>
        /// The release date is derived from the lead time rather than supplied, so the two can
        /// never disagree.
        /// </summary>
        [Fact]
        public void The_release_date_is_the_required_date_less_the_lead_time()
        {
            var line = CreateLine();

            line.Suggest(PlannedOrderAction.Purchase, 120m, leadTimeDays: 21);

            Assert.Equal(new DateOnly(2026, 2, 8), line.SuggestedReleaseDate);
            Assert.True(line.NeedsAction);
        }

        [Fact]
        public void An_action_that_orders_nothing_is_refused()
        {
            var line = CreateLine();

            Assert.Throws<ArgumentException>(() => line.Suggest(PlannedOrderAction.Purchase, 0m, 5));
        }

        [Fact]
        public void A_suggestion_cannot_be_released_twice()
        {
            var line = CreateLine();
            line.Suggest(PlannedOrderAction.Purchase, 120m, 21);

            line.MarkAsReleased("PurchaseRequisition", "REQ-9");

            Assert.True(line.IsReleased);
            Assert.Throws<InvalidOperationException>(() => line.MarkAsReleased("PurchaseRequisition", "REQ-10"));
        }

        [Fact]
        public void A_line_that_suggests_nothing_cannot_be_released()
        {
            var line = CreateLine(gross: 10m, opening: 500m);

            Assert.Throws<InvalidOperationException>(() => line.MarkAsReleased("PurchaseRequisition", "REQ-9"));
        }

        [Fact]
        public void A_release_date_already_gone_by_is_past_due()
        {
            var line = CreateLine();
            line.Suggest(PlannedOrderAction.Purchase, 120m, leadTimeDays: 21);

            Assert.True(line.IsPastDue(new DateOnly(2026, 2, 20)));
            Assert.False(line.IsPastDue(new DateOnly(2026, 2, 1)));
        }

        [Fact]
        public void A_released_line_is_never_past_due()
        {
            var line = CreateLine();
            line.Suggest(PlannedOrderAction.Purchase, 120m, leadTimeDays: 21);
            line.MarkAsReleased("PurchaseRequisition", "REQ-9");

            Assert.False(line.IsPastDue(new DateOnly(2026, 2, 20)));
        }

        [Fact]
        public void Completing_a_run_announces_how_many_lines_are_short()
        {
            var plan = CreateRunningPlan();
            var shortLine = CreateLine(gross: 100m, plan: plan);
            shortLine.Suggest(PlannedOrderAction.Purchase, 100m, 7);
            plan.AddLine(shortLine);
            plan.AddLine(CreateLine(gross: 10m, opening: 500m, plan: plan));

            plan.Complete();

            var completed = Assert.Single(plan.DomainEvents.OfType<MaterialRequirementPlanCompleted>());
            Assert.Equal(1, completed.ShortageLineCount);
            Assert.True(plan.IsActionable);
        }

        [Fact]
        public void Lines_cannot_be_written_to_a_finished_run()
        {
            var plan = CreateRunningPlan();
            plan.Complete();

            Assert.Throws<InvalidOperationException>(() => plan.AddLine(CreateLine(plan: plan)));
        }

        /// <summary>
        /// Planning against raw calendar hours is how an overload gets missed. A centre that only
        /// converts 80 per cent of its hours into work has 32 usable hours in a 40 hour week.
        /// </summary>
        [Fact]
        public void Efficiency_reduces_the_hours_that_can_actually_be_planned()
        {
            var plan = new CapacityPlan("CAP-1", DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31)), ForecastBucket.Weekly);
            var line = new CapacityPlanLine(plan, new WorkCenter("WC-1", "Assembly"), new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11), availableHours: 40m);
            line.SetEfficiency(Percentage.Create(80m));

            line.RecordLoad(firmLoadHours: 30m, plannedLoadHours: 6m);

            Assert.Equal(32m, line.EffectiveHours);
            Assert.Equal(36m, line.TotalLoadHours);
            Assert.True(line.IsOverloaded);
            Assert.Equal(4m, line.OverloadHours);
        }

        [Fact]
        public void A_plan_with_no_overloaded_bucket_is_feasible()
        {
            var plan = new CapacityPlan("CAP-2", DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31)), ForecastBucket.Weekly);
            var line = new CapacityPlanLine(plan, new WorkCenter("WC-1", "Assembly"), new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11), availableHours: 40m);
            line.RecordLoad(20m, 10m);
            plan.AddLine(line);

            plan.Complete();

            Assert.True(plan.IsFeasible);
            Assert.Empty(plan.OverloadedLines);
        }

        [Fact]
        public void A_forecast_line_outside_the_horizon_is_refused()
        {
            var forecast = new DemandForecast(
                "FC-1",
                "First half",
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)),
                ForecastBucket.Monthly,
                ForecastMethod.MovingAverage);

            var outside = new DemandForecastLine(forecast, CreateItem(), new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), 50m, Unit);

            Assert.Throws<InvalidOperationException>(() => forecast.AddLine(outside));
        }

        [Fact]
        public void An_empty_forecast_cannot_be_approved()
        {
            var forecast = new DemandForecast(
                "FC-2",
                "First half",
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)),
                ForecastBucket.Monthly,
                ForecastMethod.Manual);

            Assert.Throws<InvalidOperationException>(() => forecast.Approve());
        }

        [Fact]
        public void Variance_is_positive_when_demand_was_underestimated()
        {
            var forecast = new DemandForecast(
                "FC-3",
                "First half",
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)),
                ForecastBucket.Monthly,
                ForecastMethod.Manual);
            var line = new DemandForecastLine(forecast, CreateItem(), new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28), 100m, Unit);

            line.RecordActual(130m);

            Assert.Equal(30m, line.Variance);
        }
    }
}
