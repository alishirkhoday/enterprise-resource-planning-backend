namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// When a debt falls due, and what discount is offered for paying early. Quotations, orders and
/// invoices all inherit their due date from one of these.
/// </summary>
public sealed class PaymentTerm : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    /// <summary>Days from the invoice date until the full amount is due. Zero means cash on delivery.</summary>
    public int NetDays { get; private set; }

    /// <summary>Days within which the early settlement discount applies.</summary>
    public int? DiscountDays { get; private set; }
    public Percentage? DiscountPercentage { get; private set; }

    /// <summary>Days after the due date before the debt is treated as overdue.</summary>
    public int GraceDays { get; private set; }

    public string? Description { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }

    private PaymentTerm() { }

    public PaymentTerm(string code, string name, int netDays)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegative(netDays, nameof(netDays));
        Code = code.Trim();
        Name = name.Trim();
        NetDays = netDays;
        IsActive = true;
    }

    public void Update(string name, int netDays)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegative(netDays, nameof(netDays));
        Name = name.Trim();
        NetDays = netDays;
    }

    public void SetEarlySettlementDiscount(int discountDays, Percentage discountPercentage)
    {
        ArgumentNullException.ThrowIfNull(discountPercentage, nameof(discountPercentage));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(discountDays, nameof(discountDays));
        if (discountDays > NetDays)
            throw new ArgumentOutOfRangeException(nameof(discountDays), "The discount window cannot outlast the payment term.");
        DiscountDays = discountDays;
        DiscountPercentage = discountPercentage;
    }

    public void ClearEarlySettlementDiscount()
    {
        DiscountDays = null;
        DiscountPercentage = null;
    }

    public void SetGraceDays(int graceDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(graceDays, nameof(graceDays));
        GraceDays = graceDays;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void MarkAsDefault() => IsDefault = true;
    public void UnmarkAsDefault() => IsDefault = false;

    public DateOnly DueDateFrom(DateOnly issueDate) => issueDate.AddDays(NetDays);

    public DateOnly? DiscountDeadlineFrom(DateOnly issueDate) =>
        DiscountDays.HasValue ? issueDate.AddDays(DiscountDays.Value) : null;

    /// <summary>The discount earned by settling on the given date, if any.</summary>
    public Money? EarlySettlementDiscountOn(DateOnly issueDate, DateOnly settlementDate, Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        var deadline = DiscountDeadlineFrom(issueDate);
        if (deadline is null || DiscountPercentage is null || settlementDate > deadline.Value)
            return null;
        return amount.Percent(DiscountPercentage);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
