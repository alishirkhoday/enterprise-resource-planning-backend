namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// An agreement to pay for an order over time, with a schedule that is laid out once and then
/// paid down instalment by instalment.
/// </summary>
public sealed class InstallmentPlan : BaseEntity
{
    public Order Order { get; private set; } = default!;
    public Guid OrderId { get; private set; }

    public string? Number { get; private set; }
    public InstallmentPlanStatus Status { get; private set; }

    public Money TotalAmount { get; private set; } = default!;
    public Money DownPayment { get; private set; } = default!;
    public Percentage InterestRate { get; private set; } = default!;

    public int InstallmentCount { get; private set; }
    public DateOnly FirstDueDate { get; private set; }

    public Money PaidAmount { get; private set; } = default!;
    public Money FinancedAmount => TotalAmount.Subtract(DownPayment);
    public Money TotalPayable => FinancedAmount.Add(FinancedAmount.Percent(InterestRate)).Add(DownPayment);
    public Money OutstandingAmount => TotalPayable.Subtract(PaidAmount);

    private readonly List<Installment> _installments = [];
    public IReadOnlyList<Installment> Installments => _installments;

    private InstallmentPlan() { }

    public InstallmentPlan(Order order, Money totalAmount, int installmentCount, DateOnly firstDueDate)
    {
        ArgumentNullException.ThrowIfNull(order, nameof(order));
        ArgumentNullException.ThrowIfNull(totalAmount, nameof(totalAmount));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(installmentCount, nameof(installmentCount));
        if (!totalAmount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "A plan must cover a positive amount.");

        Order = order;
        OrderId = order.Id;
        TotalAmount = totalAmount;
        InstallmentCount = installmentCount;
        FirstDueDate = firstDueDate;
        DownPayment = Money.Zero(totalAmount.CurrencyCode);
        PaidAmount = Money.Zero(totalAmount.CurrencyCode);
        InterestRate = Percentage.Zero();
        Status = InstallmentPlanStatus.Draft;
    }

    public void SetNumber(string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        Number = number.Trim();
    }

    public void SetDownPayment(Money downPayment)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(downPayment, nameof(downPayment));
        if (downPayment.CurrencyCode != TotalAmount.CurrencyCode)
            throw new ArgumentException($"The down payment must be expressed in {TotalAmount.CurrencyCode}.", nameof(downPayment));
        if (downPayment.Amount > TotalAmount.Amount)
            throw new ArgumentOutOfRangeException(nameof(downPayment), "A down payment cannot exceed the total.");
        DownPayment = downPayment;
    }

    public void SetInterestRate(Percentage interestRate)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(interestRate, nameof(interestRate));
        InterestRate = interestRate;
    }

    /// <summary>Lays out an even schedule, with the last instalment absorbing any rounding.</summary>
    public void Activate()
    {
        EnsureDraft();
        var financedTotal = TotalPayable.Subtract(DownPayment);
        var perInstallment = financedTotal.Divide(InstallmentCount).Round(0);
        var allocated = Money.Zero(TotalAmount.CurrencyCode);

        for (var index = 1; index <= InstallmentCount; index++)
        {
            var amount = index == InstallmentCount ? financedTotal.Subtract(allocated) : perInstallment;
            allocated = allocated.Add(amount);
            _installments.Add(new Installment(this, (short)index, FirstDueDate.AddMonths(index - 1), amount));
        }

        PaidAmount = DownPayment;
        Status = InstallmentPlanStatus.Active;
    }

    public void Pay(short installmentNumber, Money amount, DateOnly paidOn, Guid? paymentId = null)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (Status != InstallmentPlanStatus.Active)
            throw new InvalidOperationException($"Payments only apply to an active plan; this one is {Status}.");

        var installment = _installments.FirstOrDefault(i => i.Number == installmentNumber)
            ?? throw new InvalidOperationException($"Instalment {installmentNumber} does not exist on this plan.");

        installment.Pay(amount, paidOn, paymentId);
        PaidAmount = PaidAmount.Add(amount);

        if (_installments.All(i => i.IsPaid))
            Status = InstallmentPlanStatus.Settled;
    }

    public void Default(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != InstallmentPlanStatus.Active)
            throw new InvalidOperationException("Only an active plan can fall into default.");
        Status = InstallmentPlanStatus.Defaulted;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == InstallmentPlanStatus.Settled)
            throw new InvalidOperationException("A settled plan cannot be cancelled.");
        Status = InstallmentPlanStatus.Cancelled;
    }

    public IEnumerable<Installment> OverdueInstallments(DateOnly asOf) =>
        _installments.Where(i => i.IsOverdueOn(asOf));

    public Installment? NextDueInstallment() =>
        _installments.Where(i => !i.IsPaid).OrderBy(i => i.Number).FirstOrDefault();

    private void EnsureDraft()
    {
        if (Status != InstallmentPlanStatus.Draft)
            throw new InvalidOperationException($"The plan is {Status} and can no longer be edited.");
    }
}
