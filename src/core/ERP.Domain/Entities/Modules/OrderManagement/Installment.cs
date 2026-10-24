namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// One instalment of a plan: what is due, when, and whether it has been paid.
/// </summary>
public sealed class Installment : BaseEntity
{
    public InstallmentPlan Plan { get; private set; } = default!;
    public Guid PlanId { get; private set; }

    public short Number { get; private set; }
    public DateOnly DueDate { get; private set; }
    public Money Amount { get; private set; } = default!;

    public Money PaidAmount { get; private set; } = default!;
    public Money RemainingAmount => Amount.Subtract(PaidAmount);
    public bool IsPaid => RemainingAmount.IsZero;

    public DateOnly? PaidOn { get; private set; }
    public Guid? PaymentId { get; private set; }

    /// <summary>A charge added for paying late, when one is applied.</summary>
    public Money? LateFee { get; private set; }

    private Installment() { }

    internal Installment(InstallmentPlan plan, short number, DateOnly dueDate, Money amount)
    {
        ArgumentNullException.ThrowIfNull(plan, nameof(plan));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number, nameof(number));

        Plan = plan;
        PlanId = plan.Id;
        Number = number;
        DueDate = dueDate;
        Amount = amount;
        PaidAmount = Money.Zero(amount.CurrencyCode);
    }

    internal void Pay(Money amount, DateOnly paidOn, Guid? paymentId)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A payment must be a positive amount.");
        if (amount.Amount > RemainingAmount.Amount)
            throw new InvalidOperationException($"The payment of {amount} exceeds the remaining {RemainingAmount}.");

        PaidAmount = PaidAmount.Add(amount);
        if (IsPaid)
        {
            PaidOn = paidOn;
            PaymentId = paymentId == Guid.Empty ? null : paymentId;
        }
    }

    internal void ApplyLateFee(Money lateFee)
    {
        ArgumentNullException.ThrowIfNull(lateFee, nameof(lateFee));
        if (IsPaid)
            throw new InvalidOperationException("A paid instalment cannot take a late fee.");
        if (lateFee.CurrencyCode != Amount.CurrencyCode)
            throw new ArgumentException("The fee must share the instalment currency.", nameof(lateFee));
        LateFee = lateFee;
        Amount = Amount.Add(lateFee);
    }

    public bool IsOverdueOn(DateOnly date) => !IsPaid && DueDate < date;

    public int DaysOverdueOn(DateOnly date) => IsOverdueOn(date) ? date.DayNumber - DueDate.DayNumber : 0;
}
