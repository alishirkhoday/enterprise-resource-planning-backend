using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Entities.Modules.OrderManagement.ValueObjects;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Entities.Users;
using ERP.Domain.Entities.Users.ValueObjects;

namespace ERP.Test.xUnit.Domain;

public sealed class ValidationConsistencyTests
{
    private static readonly DateOnly Start = new(2026, 1, 1);
    private static Address Address() => new("IR", "Tehran", "Tehran", "1", "Street", "1", "123");

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Currency_constructor_and_update_reject_the_same_precision_without_partial_updates(int precision)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Currency("IRR", "Rial", precision));
        var currency = new Currency("IRR", "Rial", 0, "R");
        Assert.Throws<ArgumentOutOfRangeException>(() => currency.Update("Changed", precision, "X"));
        Assert.Equal("Rial", currency.Name);
        Assert.Equal("R", currency.Symbol);
        Assert.Equal(0, currency.DecimalPlaces);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    public void Working_hours_use_the_same_limits_on_construction_and_update(int hours)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkCalendar("Calendar", hours));
        var calendar = new WorkCalendar("Original", 8m);
        Assert.Throws<ArgumentOutOfRangeException>(() => calendar.Update("Changed", hours));
        Assert.Equal("Original", calendar.Name);
        Assert.Equal(8m, calendar.DailyWorkingHours);
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkCenter("WC", "Center", 1, hours));
        var center = new WorkCenter("WC", "Original", 1, 8m);
        Assert.Throws<ArgumentOutOfRangeException>(() => center.Update("Changed", 2, hours));
        Assert.Equal("Original", center.Name);
        Assert.Equal(1, center.Capacity);
        Assert.Equal(8m, center.DailyCapacityHours);
    }

    [Fact]
    public void Whitespace_is_rejected_before_normalization()
    {
        Assert.Throws<ArgumentException>(() => Address().WithUnit("2").WithGeographicCoordinates(" ", "51"));
        Assert.Throws<ArgumentException>(() => new Address(" ", "P", "C", "R", "S", "1", "1"));
        Assert.Throws<ArgumentException>(() => new Image(" ", "/picture.png", "Picture"));
        Assert.Throws<ArgumentException>(() => new Image("picture.png", "/picture.png", " "));
    }

    [Fact]
    public void A_negative_opportunity_update_leaves_all_original_fields_intact()
    {
        var opportunity = new Opportunity("Original", Money.Create(100m, "IRR"), Start);
        Assert.Throws<ArgumentOutOfRangeException>(() => opportunity.Update("Changed", Money.Create(-1m, "IRR"), Start.AddDays(1)));
        Assert.Equal("Original", opportunity.Title);
        Assert.Equal(100m, opportunity.EstimatedValue.Amount);
        Assert.Equal(Start, opportunity.ExpectedCloseDate);
    }

    [Fact]
    public void Failed_payment_term_changes_leave_both_reference_and_date_intact()
    {
        var originalId = Guid.NewGuid();
        var receivable = new Receivable(Guid.NewGuid(), Money.Create(100m, "IRR"), Start, Start.AddDays(30));
        var payable = new Payable(Guid.NewGuid(), Money.Create(100m, "IRR"), Start, Start.AddDays(30));
        receivable.ApplyPaymentTerm(originalId, Start.AddDays(20));
        payable.ApplyPaymentTerm(originalId, Start.AddDays(20));
        Assert.Throws<ArgumentOutOfRangeException>(() => receivable.ApplyPaymentTerm(Guid.NewGuid(), Start.AddDays(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => payable.ApplyPaymentTerm(Guid.NewGuid(), Start.AddDays(-1)));
        Assert.Equal(originalId, receivable.PaymentTermId);
        Assert.Equal(originalId, payable.PaymentTermId);
        Assert.Equal(Start.AddDays(20), receivable.DueDate);
        Assert.Equal(Start.AddDays(20), payable.DueDate);
    }

    [Fact]
    public void Shortening_a_payment_term_cannot_invalidate_its_discount_window()
    {
        var term = new PaymentTerm("NET30", "Original", 30);
        term.SetEarlySettlementDiscount(10, Percentage.Create(2m));
        Assert.Throws<ArgumentOutOfRangeException>(() => term.Update("Changed", 5));
        Assert.Equal("Original", term.Name);
        Assert.Equal(30, term.NetDays);
        Assert.Equal(10, term.DiscountDays);
    }

    [Fact]
    public void Document_updates_revalidate_year_and_recompute_accounting_period_atomically()
    {
        var year = new FinancialYear("2026", Start, new(2026, 12, 31));
        var first = new AccountingPeriod(year, 1, "First", DateRange.Create(Start, new(2026, 6, 30)));
        var second = new AccountingPeriod(year, 2, "Second", DateRange.Create(new(2026, 7, 1), new(2026, 12, 31)));
        year.AddPeriod(first);
        year.AddPeriod(second);
        var originalTime = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var document = new Document(year, "Original", originalTime, "Description", DocumentType.Receive);
        Assert.Throws<ArgumentOutOfRangeException>(() => document.Update("Changed", originalTime.AddYears(1), "Changed", DocumentType.Payment));
        Assert.Equal("Original", document.Title);
        Assert.Equal("Description", document.Description);
        Assert.Equal(originalTime, document.FinancialEventDateTime);
        Assert.Equal(first.Id, document.AccountingPeriodId);
        document.Update("Updated", originalTime.AddMonths(6), "Updated", DocumentType.Receive);
        Assert.Equal(second.Id, document.AccountingPeriodId);
    }

    [Fact]
    public void Replacing_weekends_materializes_the_input_before_changing_the_calendar()
    {
        var calendar = new WorkCalendar("Calendar");
        calendar.SetWeekendDays([DayOfWeek.Friday]);
        calendar.SetWeekendDays(calendar.WeekendDays);
        Assert.Equal(DayOfWeek.Friday, Assert.Single(calendar.WeekendDays));
        Assert.Throws<ArgumentException>(() => calendar.SetWeekendDays([DayOfWeek.Saturday, (DayOfWeek)99]));
        Assert.Equal(DayOfWeek.Friday, Assert.Single(calendar.WeekendDays));
    }

    [Fact]
    public void Address_copies_do_not_change_shared_snapshots_or_dictionary_keys()
    {
        var address = Address();
        var shipping = new ShipToAddress(address, "Receiver", "09120000000");
        var values = new Dictionary<Address, string> { [address] = "Original" };
        var updated = address.WithUnit(" 2 ").WithGeographyReferences(Guid.NewGuid(), null, null)
            .WithGeographicCoordinates("35.7", "51.4");
        Assert.Null(address.Unit);
        Assert.Null(shipping.Address.Latitude);
        Assert.Null(address.CountryId);
        Assert.Equal("Original", values[address]);
        Assert.Equal("2", updated.Unit);
        Assert.Equal("35.7", updated.Latitude);
        Assert.NotEqual(address, updated);
        Assert.Throws<ArgumentOutOfRangeException>(() => address.WithGeographicCoordinates("91", "51"));
        Assert.Null(address.Latitude);
    }

    [Fact]
    public void Every_address_component_and_delivery_instruction_participates_in_value_equality()
    {
        var address = Address();
        Assert.Equal(address, Address());
        Assert.NotEqual(address, address.WithGeographyReferences(Guid.NewGuid(), null, null));
        var shipping = new ShipToAddress(address, "Receiver", "09120000000");
        var changed = shipping.WithDeliveryInstruction(" Ring bell ").WithReceiverNationalId(" 123 ");
        Assert.Null(shipping.DeliveryInstruction);
        Assert.Null(shipping.ReceiverNationalId);
        Assert.Equal("Ring bell", changed.DeliveryInstruction);
        Assert.Equal("123", changed.ReceiverNationalId);
        Assert.NotEqual(shipping, shipping.WithDeliveryInstruction("Ring bell"));
    }

    [Fact]
    public void Deleting_a_profile_image_releases_the_reference_without_mutating_a_shared_value()
    {
        var image = new Image("picture.png", "/picture.png", "Picture");
        var first = new UserProfile(new User("first", "hash"));
        var second = new UserProfile(new User("second", "hash"));
        first.SetImage(image);
        second.SetImage(image);
        first.DeleteImage();
        first.DeleteImage();
        Assert.Null(first.Image);
        Assert.Equal(image, second.Image);
        Assert.Equal("picture.png", image.FileName);
        Assert.Throws<ArgumentNullException>(() => second.SetImage(null!));
        Assert.Equal(image, second.Image);
    }
}
