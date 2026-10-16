namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// One instalment of a loan - قسط. The row that a payment is matched against and that an
/// overdue report reads.
/// </summary>
public sealed class LoanInstallment : BaseEntity
{
    public Loan Loan { get; private set; } = default!;
    public Guid LoanId { get; private set; }

    public short Number { get; private set; }
    public DateOnly DueDate { get; private set; }
    public Money Amount { get; private set; } = default!;

    public Money PaidAmount { get; private set; } = default!;
    public Money RemainingAmount => Amount.Subtract(PaidAmount);
    public bool IsPaid => RemainingAmount.IsZero;

    public DateOnly? PaidOn { get; private set; }
    public Guid? PayrollId { get; private set; }

    private LoanInstallment() { }

    internal LoanInstallment(Loan loan, short number, DateOnly dueDate, Money amount)
    {
        ArgumentNullException.ThrowIfNull(loan, nameof(loan));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number, nameof(number));

        Loan = loan;
        LoanId = loan.Id;
        Number = number;
        DueDate = dueDate;
        Amount = amount;
        PaidAmount = Money.Zero(amount.CurrencyCode);
    }

    internal void Pay(Money amount, DateOnly paidOn, Guid? payrollId)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A repayment must be a positive amount.");
        if (amount.Amount > RemainingAmount.Amount)
            throw new InvalidOperationException($"The repayment of {amount} exceeds the remaining {RemainingAmount}.");

        PaidAmount = PaidAmount.Add(amount);
        if (IsPaid)
        {
            PaidOn = paidOn;
            PayrollId = payrollId == Guid.Empty ? null : payrollId;
        }
    }

    public bool IsOverdueOn(DateOnly date) => !IsPaid && DueDate < date;
}
