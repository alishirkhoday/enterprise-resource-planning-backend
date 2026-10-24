using System.Collections;
using System.Reflection;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.OrderManagement;
using ERP.Domain.Entities.Modules.OrderManagement.ValueObjects;
using ERP.Domain.Entities.Users;
using ERP.Domain.Repositories;
using ERP.Infrastructure.MainDatabase.Repositories;

namespace ERP.Test.xUnit.Domain;

public sealed class AggregateBoundaryTests
{
    private static readonly Guid Unit = Guid.NewGuid();
    private static Customer Customer() => new("C-1", "Customer", CustomerType.Organization);
    private static Order Order() => new(Customer(), "O-1", "IRR");
    private static Item Item() => new(new ItemGroup("G-1", "Goods"), ItemType.Part);
    private static OrderItem Line(Order order, string currency = "IRR") => new(order, Item(), 1, Money.Create(100m, currency), 1m, Unit);
    private static FinancialYear Year() => new("2026", new(2026, 1, 1), new(2026, 12, 31));
    private static Document Document(FinancialYear year) => new(year, "Entry", new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero), "Description", DocumentType.Receive);
    private static Account Account() => new("1001", "Cash", AccountType.AssetCurrent, AccountGroup.LevelFour,
        AccountBalance.Debtor, FinancialStatement.BalanceSheet, true);

    [Fact]
    public void Only_roots_can_be_used_as_repository_arguments()
    {
        Assert.False(typeof(IAggregateRoot<Guid>).IsAssignableFrom(typeof(BaseEntity)));
        Assert.True(typeof(IAggregateRoot<Guid>).IsAssignableFrom(typeof(Order)));
        Assert.False(typeof(IAggregateRoot<Guid>).IsAssignableFrom(typeof(OrderItem)));
        Assert.Throws<ArgumentException>(() => typeof(IRepository<>).MakeGenericType(typeof(OrderItem)));
        Assert.Throws<ArgumentException>(() => typeof(Repository<>).MakeGenericType(typeof(DocumentItem)));
    }

    [Fact]
    public void Owned_entities_expose_no_public_business_mutators()
    {
        var children = typeof(Order).Assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }
            && typeof(BaseEntity).IsAssignableFrom(t) && !typeof(IAggregateRoot<Guid>).IsAssignableFrom(t)).ToArray();
        Assert.NotEmpty(children);
        Assert.All(children, child => Assert.DoesNotContain(child.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            method => method.ReturnType == typeof(void)));
        Assert.Null(typeof(OrderItem).GetMethod("Delete"));
        Assert.Null(typeof(OrderItem).GetMethod("Restore"));
    }

    [Fact]
    public void Collection_views_and_events_reject_changes_through_collection_interfaces()
    {
        var order = Order();
        var line = Line(order);
        order.AddItem(line);
        order.Place();
        Assert.Throws<NotSupportedException>(() => ((IList)order.Items).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList)order.DomainEvents).Clear());
        Assert.Same(line, Assert.Single(order.Items));
        Assert.Single(order.DomainEvents);
    }

    [Fact]
    public void Foreign_duplicate_and_unattached_lines_are_rejected_before_any_change()
    {
        var order = Order();
        var foreign = Line(Order());
        Assert.Throws<InvalidOperationException>(() => order.AddItem(foreign));
        Assert.Empty(order.Items);
        var line = Line(order);
        Assert.Throws<InvalidOperationException>(() => order.SetOrderItemDiscount(line.Id, Money.Create(10m, "IRR")));
        order.AddItem(line);
        Assert.Throws<InvalidOperationException>(() => order.AddItem(line));
        Assert.Single(order.Items);
        Assert.True(line.Discount.IsZero);
    }

    [Fact]
    public void Replacing_order_lines_is_atomic_and_can_accept_its_own_view()
    {
        var order = Order();
        var original = Line(order);
        order.AddItem(original);
        order.SetItems(order.Items);
        Assert.Throws<InvalidOperationException>(() => order.SetItems([Line(order), Line(order, "USD")]));
        Assert.Same(original, Assert.Single(order.Items));
        Assert.Throws<InvalidOperationException>(() => order.SetItems([original, original]));
        Assert.Same(original, Assert.Single(order.Items));
    }

    [Fact]
    public void Cancelling_an_order_closes_line_commands()
    {
        var order = Order();
        var line = Line(order);
        order.AddItem(line);
        order.Cancel("Cancelled");
        Assert.Throws<InvalidOperationException>(() => order.SetOrderItemTax(line.Id, Guid.NewGuid(), Percentage.Create(9m)));
        Assert.Throws<InvalidOperationException>(() => order.SetOrderItemDiscount(line.Id, Money.Create(10m, "IRR")));
        Assert.True(line.Discount.IsZero);
        Assert.Equal(0m, line.TaxPercentage.Value);
    }

    [Fact]
    public void A_confirmed_order_cannot_change_its_line_amounts()
    {
        var order = Order();
        var line = Line(order);
        order.AddItem(line);
        var payment = new Payment(order, PaymentMethod.Cash, Money.Create(100m, "IRR"));
        order.AddPayment(payment);
        order.MarkPaymentAsSucceeded(payment.Id, Money.Create(100m, "IRR"), "TX-1");
        order.Confirm();
        Assert.Throws<InvalidOperationException>(() => order.SetOrderItemDiscount(line.Id, Money.Create(10m, "IRR")));
        Assert.Equal(100m, order.AmountPayable.Amount);
    }

    [Fact]
    public void Payments_and_refunds_cannot_be_attached_to_a_different_owner_or_twice()
    {
        var order = Order();
        var payment = new Payment(order, PaymentMethod.Cash, Money.Create(100m, "IRR"));
        Assert.Throws<InvalidOperationException>(() => Order().AddPayment(payment));
        order.AddPayment(payment);
        order.MarkPaymentAsSucceeded(payment.Id, Money.Create(100m, "IRR"), "TX");
        var refund = new Refund(payment, Money.Create(30m, "IRR"), (RefundReason)0);
        order.AddPaymentRefund(payment.Id, refund);
        Assert.Throws<InvalidOperationException>(() => order.AddPaymentRefund(payment.Id, refund));
        order.AddPaymentRefund(payment.Id, new Refund(payment, Money.Create(20m, "IRR"), (RefundReason)0));
        Assert.Equal(50m, payment.RefundedAmount.Amount);
        Assert.Equal(2, payment.Refunds.Count);
        var otherPayment = new Payment(order, PaymentMethod.Cash, Money.Create(100m, "IRR"));
        order.AddPayment(otherPayment);
        order.MarkPaymentAsSucceeded(otherPayment.Id, Money.Create(100m, "IRR"), "OTHER");
        Assert.Throws<InvalidOperationException>(() => order.AddPaymentRefund(otherPayment.Id, refund));
        Assert.Empty(otherPayment.Refunds);
        Assert.True(otherPayment.RefundedAmount.IsZero);
    }

    [Fact]
    public void Invoice_replacement_and_issue_preserve_the_boundary()
    {
        var invoice = new Invoice(Customer(), "I-1", InvoiceType.Sales, new(2026, 1, 1), new(2026, 2, 1), "IRR");
        var line = new InvoiceItem(invoice, Item(), 1, Money.Create(100m, "IRR"), 1m, Unit);
        invoice.AddItem(line);
        Assert.Throws<InvalidOperationException>(() => invoice.SetItems([new InvoiceItem(invoice, Item(), 2, Money.Create(100m, "USD"), 1m, Unit)]));
        Assert.Same(line, Assert.Single(invoice.Items));
        invoice.Issue();
        Assert.Throws<InvalidOperationException>(() => invoice.SetInvoiceItemDiscount(line.Id, Money.Create(10m, "IRR")));
    }

    [Fact]
    public void Posted_document_lines_and_status_cannot_bypass_posting_rules()
    {
        var year = Year();
        year.AddPeriod(new AccountingPeriod(year, 1, "Year", DateRange.Create(new(2026, 1, 1), new(2026, 12, 31))));
        year.Open(Guid.NewGuid());
        var document = Document(year);
        var debit = new DocumentItem(document, Account(), 1, 100m, 0m);
        document.AddItem(debit);
        Assert.Throws<InvalidOperationException>(() => document.ChangeStatus(DocumentStatus.Definite));
        Assert.Equal(DocumentStatus.Draft, document.Status);
        document.AddItem(new DocumentItem(document, Account(), 2, 0m, 100m));
        document.Post();
        Assert.Throws<InvalidOperationException>(() => document.SetDocumentItemDescription(debit.Id, "Changed"));
        Assert.Throws<InvalidOperationException>(() => document.ChangeStatus(DocumentStatus.Draft));
        Assert.Null(debit.Description);
    }

    [Fact]
    public void Changing_the_default_address_coordinates_the_siblings()
    {
        var customer = Customer();
        var address = new Address("IR", "Tehran", "Tehran", "1", "Street", "1", "123");
        var first = new CustomerAddress(customer, address);
        var second = new CustomerAddress(customer, address.WithUnit("2"));
        customer.AddAddress(first);
        customer.AddAddress(second);
        customer.SetCustomerAddressDefault(second.Id);
        Assert.False(first.IsDefault);
        Assert.True(second.IsDefault);
        Assert.Throws<InvalidOperationException>(() => customer.SetCustomerAddressDefault(Guid.NewGuid()));
        Assert.True(second.IsDefault);
    }
}
