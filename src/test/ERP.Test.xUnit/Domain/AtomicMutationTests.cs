using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.OrderManagement;
using ERP.Domain.Entities.Modules.PlanningManagement;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Entities.Users;

namespace ERP.Test.xUnit.Domain;

public sealed class AtomicMutationTests
{
    private static Item Item() => new(new ItemGroup("G", "Goods"), ItemType.Part);

    [Fact]
    public void An_invalid_phone_number_does_not_replace_the_existing_region_or_number()
    {
        var user = new User("user", "hash");
        user.SetMobilePhoneNumber("98", "9121234567");
        var normalized = user.NormalizedMobilePhoneNumber;
        Assert.Throws<ArgumentException>(() => user.SetMobilePhoneNumber("44", "x"));
        Assert.Equal("98", user.MobilePhoneNumberRegionCode.Value);
        Assert.Equal("9121234567", user.MobilePhoneNumber.Value);
        Assert.Equal(normalized, user.NormalizedMobilePhoneNumber);
    }

    [Fact]
    public void A_cost_currency_error_does_not_apply_earlier_cost_components()
    {
        var item = Item();
        item.SetUsage(true, true, true);
        var order = new ProductionOrder("P-1", item, 1m, "IRR");
        Assert.Throws<InvalidOperationException>(() => order.AccumulateCost(
            Money.Create(100m, "IRR"), Money.Create(20m, "IRR"), Money.Create(5m, "USD")));
        Assert.True(order.MaterialCost.IsZero);
        Assert.True(order.LabourCost.IsZero);
        Assert.True(order.OverheadCost.IsZero);
    }

    [Fact]
    public void A_release_date_error_does_not_leave_half_of_a_planning_suggestion()
    {
        var plan = new MaterialRequirementPlan("P-1", DateRange.Create(DateOnly.MinValue, new(2026, 12, 31)));
        var line = new MaterialRequirementPlanLine(plan, Item(), DateOnly.MinValue, 10m, 0m, 0m, Guid.NewGuid());
        plan.AddLine(line);
        Assert.Throws<ArgumentOutOfRangeException>(() => plan.SuggestMaterialRequirementPlanLine(line.Id,
            PlannedOrderAction.Purchase, 10m, 1, Guid.NewGuid()));
        Assert.Equal(PlannedOrderAction.None, line.SuggestedAction);
        Assert.Equal(0m, line.PlannedOrderQuantity);
        Assert.Equal(0, line.LeadTimeDays);
        Assert.Null(line.SupplierId);
    }

    [Fact]
    public void A_schedule_date_error_does_not_attach_partial_installments()
    {
        var order = new Order(new Customer("C-1", "Customer", CustomerType.Organization), "O-1", "IRR");
        var plan = new InstallmentPlan(order, Money.Create(100m, "IRR"), 2, new(9999, 12, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => plan.Activate());
        Assert.Empty(plan.Installments);
        Assert.Equal(InstallmentPlanStatus.Draft, plan.Status);
        Assert.True(plan.PaidAmount.IsZero);
    }
}
