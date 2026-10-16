using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Money advanced to an employee and recovered through payroll instalments. It is a receivable
/// of a particular shape, which is why the schedule lives with it.
/// </summary>
public sealed class Loan : BaseEntity
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public string? Number { get; private set; }
    public LoanType Type { get; private set; }
    public LoanStatus Status { get; private set; }

    public Money PrincipalAmount { get; private set; } = default!;
    public Percentage InterestRate { get; private set; } = default!;
    public int InstallmentCount { get; private set; }

    public DateOnly GrantedOn { get; private set; }
    public DateOnly FirstInstallmentDate { get; private set; }

    public Money RepaidAmount { get; private set; } = default!;
    public Money TotalPayable => PrincipalAmount.Add(PrincipalAmount.Percent(InterestRate));
    public Money OutstandingAmount => TotalPayable.Subtract(RepaidAmount);

    public Guid? AccountId { get; private set; }
    public Guid? SettlementId { get; private set; }
    public string? Description { get; private set; }

    private readonly List<LoanInstallment> _installments = [];
    public IReadOnlyList<LoanInstallment> Installments => _installments;

    private Loan() { }

    public Loan(Human human, LoanType type, Money principalAmount, int installmentCount, DateOnly grantedOn, DateOnly firstInstallmentDate)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentNullException.ThrowIfNull(principalAmount, nameof(principalAmount));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(installmentCount, nameof(installmentCount));
        if (!principalAmount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(principalAmount), "A loan must be a positive amount.");
        if (firstInstallmentDate < grantedOn)
            throw new ArgumentOutOfRangeException(nameof(firstInstallmentDate), "The first instalment cannot fall before the loan is granted.");

        Human = human;
        HumanId = human.Id;
        Type = type;
        PrincipalAmount = principalAmount;
        InstallmentCount = installmentCount;
        GrantedOn = grantedOn;
        FirstInstallmentDate = firstInstallmentDate;
        InterestRate = Percentage.Zero();
        RepaidAmount = Money.Zero(principalAmount.CurrencyCode);
        Status = LoanStatus.Requested;
    }

    public void SetNumber(string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        Number = number.Trim();
    }

    public void SetInterestRate(Percentage rate)
    {
        if (Status != LoanStatus.Requested)
            throw new InvalidOperationException("The rate can only be set before the loan is approved.");
        ArgumentNullException.ThrowIfNull(rate, nameof(rate));
        InterestRate = rate;
    }

    public void LinkAccount(Guid accountId) => AccountId = accountId == Guid.Empty ? null : accountId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>Approves the loan and lays out an even repayment schedule.</summary>
    public void Approve()
    {
        if (Status != LoanStatus.Requested)
            throw new InvalidOperationException($"Only a requested loan can be approved; this one is {Status}.");

        var perInstallment = TotalPayable.Divide(InstallmentCount).Round(0);
        var allocated = Money.Zero(PrincipalAmount.CurrencyCode);

        for (var index = 1; index <= InstallmentCount; index++)
        {
            // The final instalment absorbs any rounding so the schedule adds up exactly.
            var amount = index == InstallmentCount ? TotalPayable.Subtract(allocated) : perInstallment;
            allocated = allocated.Add(amount);
            _installments.Add(new LoanInstallment(this, (short)index, FirstInstallmentDate.AddMonths(index - 1), amount));
        }

        Status = LoanStatus.Approved;
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status != LoanStatus.Requested)
            throw new InvalidOperationException($"Only a requested loan can be rejected; this one is {Status}.");
        Status = LoanStatus.Rejected;
        Description = reason.Trim();
    }

    public void Disburse(Guid settlementId)
    {
        if (Status != LoanStatus.Approved)
            throw new InvalidOperationException("Only an approved loan can be disbursed.");
        SettlementId = settlementId == Guid.Empty ? null : settlementId;
        Status = LoanStatus.Active;
    }

    /// <summary>Records repayment of one instalment, normally withheld through payroll.</summary>
    public void Repay(short installmentNumber, Money amount, DateOnly paidOn, Guid? payrollId = null)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (Status != LoanStatus.Active)
            throw new InvalidOperationException($"Repayments only apply to an active loan; this one is {Status}.");

        var installment = _installments.FirstOrDefault(i => i.Number == installmentNumber)
            ?? throw new InvalidOperationException($"Instalment {installmentNumber} does not exist on this loan.");

        installment.Pay(amount, paidOn, payrollId);
        RepaidAmount = RepaidAmount.Add(amount);

        if (_installments.All(i => i.IsPaid))
            Status = LoanStatus.Settled;
    }

    public LoanInstallment? NextDueInstallment(DateOnly asOf) =>
        _installments.Where(i => !i.IsPaid && i.DueDate <= asOf).OrderBy(i => i.Number).FirstOrDefault();

    public IEnumerable<LoanInstallment> OverdueInstallments(DateOnly asOf) =>
        _installments.Where(i => !i.IsPaid && i.DueDate < asOf);

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (!RepaidAmount.IsZero)
            throw new InvalidOperationException("A loan with repayments cannot be cancelled.");
        Status = LoanStatus.Cancelled;
        Description = reason.Trim();
    }
}
