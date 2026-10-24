using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.OrderManagement;
using ERP.Domain.Entities.Modules.OrderManagement.ValueObjects;
using ERP.Domain.Entities.Modules.ProjectManagement;
using ERP.Domain.Entities.Users;
using ERP.Domain.Entities.Users.ValueObjects;
using ERP.Infrastructure.MainDatabase.Context;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.OrderManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.ProjectManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ERP.Test.xUnit.Infrastructure;

public sealed class AggregatePersistenceTests
{
    private static MainDbContext Context() => new(new DbContextOptionsBuilder<MainDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void Read_only_collection_navigations_are_populated_through_their_backing_fields()
    {
        using var context = Context();
        foreach (var entity in context.Model.GetEntityTypes().Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
            foreach (var navigation in entity.GetNavigations().Where(n => n.IsCollection && n.FieldInfo is not null))
                Assert.Equal(PropertyAccessMode.Field, navigation.GetPropertyAccessMode());
        Assert.Null(context.Model.FindEntityType(typeof(AggregateRoot)));
        Assert.Null(context.Model.FindEntityType(typeof(BaseEntity)));
    }

    [Fact]
    public async Task A_loaded_order_keeps_its_children_and_replaces_its_immutable_address()
    {
        var token = TestContext.Current.CancellationToken;
        using var context = Context();
        var customer = new Customer("C-1", "Customer", CustomerType.Organization);
        var order = new Order(customer, "O-1", "IRR");
        var item = new Item(new ItemGroup("G-1", "Goods"), ItemType.Part);
        var line = new OrderItem(order, item, 1, Money.Create(100m, "IRR"), 1m, Guid.NewGuid());
        order.AddItem(line);
        var address = new Address("IR", "Tehran", "Tehran", "1", "Street", "1", "123");
        customer.AddAddress(new CustomerAddress(customer, address));
        order.SetAddress(new ShipToAddress(address, "Receiver", "09120000000"));
        context.Add(order);
        await context.SaveChangesAsync(token);
        context.ChangeTracker.Clear();

        var loaded = await new OrderRepository(context).GetWithItemsAndPaymentsAsync(order.Id, token);
        Assert.NotNull(loaded);
        Assert.Equal(line.Id, Assert.Single(loaded.Items).Id);
        var original = loaded.ShipToAddress;
        loaded.SetOrderItemDiscount(line.Id, Money.Create(10m, "IRR"));
        loaded.SetAddress(new ShipToAddress(original.Address.WithUnit("2"), original.ReceiverFullName,
            original.ReceiverMobileNumber, deliveryInstruction: "Ring bell"));
        await context.SaveChangesAsync(token);
        context.ChangeTracker.Clear();

        var reloaded = await new OrderRepository(context).GetWithItemsAndPaymentsAsync(order.Id, token);
        Assert.NotNull(reloaded);
        Assert.Equal(90m, reloaded.AmountPayable.Amount);
        Assert.Equal("2", reloaded.ShipToAddress.Address.Unit);
        Assert.Equal("Ring bell", reloaded.ShipToAddress.DeliveryInstruction);
        Assert.Null(original.Address.Unit);
    }

    [Fact]
    public async Task A_profile_can_replace_and_remove_its_optional_image_after_reloading()
    {
        var token = TestContext.Current.CancellationToken;
        using var context = Context();
        var profile = new UserProfile(new User("profile-user", "hash"));
        profile.SetImage(new Image("one.png", "/one.png", "One"));
        context.Add(profile);
        await context.SaveChangesAsync(token);
        context.ChangeTracker.Clear();
        var loaded = await context.Set<UserProfile>().SingleAsync(p => p.Id == profile.Id, token);
        loaded.SetImage(new Image("two.png", "/two.png", "Two"));
        await context.SaveChangesAsync(token);
        context.ChangeTracker.Clear();
        loaded = await context.Set<UserProfile>().SingleAsync(p => p.Id == profile.Id, token);
        Assert.Equal("two.png", loaded.Image?.FileName);
        loaded.DeleteImage();
        await context.SaveChangesAsync(token);
        context.ChangeTracker.Clear();
        loaded = await context.Set<UserProfile>().SingleAsync(p => p.Id == profile.Id, token);
        Assert.Null(loaded.Image);
    }

    [Fact]
    public async Task Looking_up_a_milestone_loads_its_owning_aggregate_for_changes()
    {
        var token = TestContext.Current.CancellationToken;
        using var context = Context();
        var period = DateRange.Create(new(2026, 1, 1), new(2026, 12, 31));
        var project = new Project("P-1", "Project", ProjectType.Customer, period, "IRR");
        var phase = new ProjectPhase(project, "Phase", 1, DateRange.Create(period.StartDate, period.EndDate), Percentage.Create(100m));
        project.AddPhase(phase);
        var milestone = new ProjectMilestone(phase, "Milestone", new(2026, 6, 1));
        project.AddProjectPhaseMilestone(phase.Id, milestone);
        context.Add(project);
        await context.SaveChangesAsync(token);
        context.ChangeTracker.Clear();
        var loaded = await new ProjectRepository(context).GetMilestoneAsync(milestone.Id, token);
        Assert.NotNull(loaded);
        Assert.Same(loaded, Assert.Single(Assert.Single(loaded.Phase.Project.Phases).Milestones));
        loaded.Phase.Project.AchieveProjectMilestone(loaded.Id, new(2026, 6, 1));
        await context.SaveChangesAsync(token);
        Assert.Equal(MilestoneStatus.Achieved, loaded.Status);
    }
}
