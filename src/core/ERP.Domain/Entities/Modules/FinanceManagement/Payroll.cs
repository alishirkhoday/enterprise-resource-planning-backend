using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// One employee pay for one period. Gross, deductions and net are all derived from the lines,
/// so the slip can always be explained item by item.
/// </summary>
public sealed partial class Payroll : AggregateRoot, IBranchScoped
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public PayrollStatus Status { get; private set; }

    public Guid? SalaryStructureId { get; private set; }
    public Guid? ContractId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string CurrencyCode { get; private set; } = default!;

    /// <summary>The contracted wage the period is computed from.</summary>
    public Money BaseSalary { get; private set; } = default!;

    public decimal WorkedDays { get; private set; }
    public decimal AbsentDays { get; private set; }
    public decimal PaidLeaveDays { get; private set; }
    public decimal UnpaidLeaveDays { get; private set; }
    public decimal OvertimeHours { get; private set; }

    public DateOnly? PaidOn { get; private set; }
    public Guid? SettlementId { get; private set; }
    public Guid? DocumentId { get; private set; }

    private readonly List<PayrollItem> _items = [];
    public IReadOnlyList<PayrollItem> Items => _items.AsReadOnly();

    private readonly List<PayrollReward> _rewards = [];
    public IReadOnlyList<PayrollReward> Rewards => _rewards.AsReadOnly();

    private readonly List<PayrollDeduction> _deductions = [];
    public IReadOnlyList<PayrollDeduction> Deductions => _deductions.AsReadOnly();

    private Payroll() { }

    public Payroll(Human human, DateOnly startDate, DateOnly endDate, Money baseSalary)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentNullException.ThrowIfNull(baseSalary, nameof(baseSalary));
        if (endDate < startDate)
            throw new ArgumentOutOfRangeException(nameof(endDate), $"EndDate cannot be earlier than StartDate.");
        if (baseSalary.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(baseSalary), "BaseSalary cannot be negative.");

        Human = human;
        HumanId = human.Id;
        StartDate = startDate;
        EndDate = endDate;
        BaseSalary = baseSalary;
        CurrencyCode = baseSalary.CurrencyCode;
        Status = PayrollStatus.Draft;
    }

    public void Update(DateOnly startDate, DateOnly endDate)
    {
        EnsureDraft();
        if (endDate < startDate)
            throw new ArgumentOutOfRangeException(nameof(endDate), $"EndDate cannot be earlier than StartDate.");
        StartDate = startDate;
        EndDate = endDate;
    }

    public void ApplyStructure(Guid salaryStructureId, Guid? contractId = null)
    {
        EnsureDraft();
        SalaryStructureId = salaryStructureId == Guid.Empty ? null : salaryStructureId;
        ContractId = contractId == Guid.Empty ? null : contractId;
    }

    public void SetBaseSalary(Money baseSalary)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(baseSalary, nameof(baseSalary));
        if (baseSalary.CurrencyCode != CurrencyCode)
            throw new ArgumentException($"The salary must be expressed in {CurrencyCode}.", nameof(baseSalary));
        BaseSalary = baseSalary;
    }

    public void RecordAttendance(decimal workedDays, decimal absentDays, decimal paidLeaveDays, decimal unpaidLeaveDays, decimal overtimeHours)
    {
        EnsureDraft();
        ArgumentOutOfRangeException.ThrowIfNegative(workedDays, nameof(workedDays));
        ArgumentOutOfRangeException.ThrowIfNegative(absentDays, nameof(absentDays));
        ArgumentOutOfRangeException.ThrowIfNegative(paidLeaveDays, nameof(paidLeaveDays));
        ArgumentOutOfRangeException.ThrowIfNegative(unpaidLeaveDays, nameof(unpaidLeaveDays));
        ArgumentOutOfRangeException.ThrowIfNegative(overtimeHours, nameof(overtimeHours));

        WorkedDays = workedDays;
        AbsentDays = absentDays;
        PaidLeaveDays = paidLeaveDays;
        UnpaidLeaveDays = unpaidLeaveDays;
        OvertimeHours = overtimeHours;
    }

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void AddItem(PayrollItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureCanAddChild(item, item.PayrollId, _items);

        EnsureDraft();
        if (item.Amount.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException($"Payroll lines must be expressed in {CurrencyCode}.");
        if (_items.Any(i => i.Code == item.Code))
            throw new InvalidOperationException($"A line with code {item.Code} already exists on this payroll.");
        _items.Add(item);
    }

    public void RemoveItem(string code)
    {
        EnsureDraft();
        var item = _items.FirstOrDefault(i => i.Code == code);
        if (item is not null)
            _items.Remove(item);
    }

    public void AddReward(PayrollReward reward)
    {
        ArgumentNullException.ThrowIfNull(reward);
        EnsureCanAddChild(reward, reward.PayrollId, _rewards);

        EnsureDraft();
        _rewards.Add(reward);
    }

    public void AddDeduction(PayrollDeduction deduction)
    {
        ArgumentNullException.ThrowIfNull(deduction);
        EnsureCanAddChild(deduction, deduction.PayrollId, _deductions);

        EnsureDraft();
        _deductions.Add(deduction);
    }

    public Money GrossPay => Sum(_items.Where(i => i.Nature == SalaryItemNature.Earning));

    public Money TotalDeductions => Sum(_items.Where(i => i.Nature == SalaryItemNature.Deduction));

    public Money EmployerContributions => Sum(_items.Where(i => i.Nature == SalaryItemNature.EmployerContribution));

    public Money NetPayable => GrossPay.Subtract(TotalDeductions);

    /// <summary>The part of gross pay that income tax is computed on.</summary>
    public Money TaxableBase => Sum(_items.Where(i => i.Nature == SalaryItemNature.Earning && i.IsTaxable));

    /// <summary>The part of gross pay that social insurance is computed on.</summary>
    public Money InsurableBase => Sum(_items.Where(i => i.Nature == SalaryItemNature.Earning && i.IsInsurable));

    /// <summary>The total cost to the employer, including contributions the employee never sees.</summary>
    public Money TotalEmployerCost => GrossPay.Add(EmployerContributions);

    /// <summary>
    /// Closes the pay slip for editing. Named Confirm rather than Finalize because a method
    /// called Finalize collides with the object destructor.
    /// </summary>
    public void Confirm()
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("A payroll cannot be finalised without any lines.");
        if (NetPayable.IsNegative)
            throw new InvalidOperationException($"The net payable is negative at {NetPayable}; review the deductions.");
        Status = PayrollStatus.Definite;
        Raise(new PayrollFinalized(Id, HumanId, NetPayable.Amount, CurrencyCode));
    }

    /// <summary>Reopens a finalised payroll for correction before it has been paid.</summary>
    public void Reopen()
    {
        if (Status != PayrollStatus.Definite)
            throw new InvalidOperationException("Only a finalised payroll can be reopened.");
        if (PaidOn is not null)
            throw new InvalidOperationException("A paid payroll cannot be reopened.");
        Status = PayrollStatus.Draft;
    }

    public void MarkAsPaid(DateOnly paidOn, Guid? settlementId = null)
    {
        if (Status != PayrollStatus.Definite)
            throw new InvalidOperationException("Only a finalised payroll can be paid.");
        PaidOn = paidOn;
        SettlementId = settlementId == Guid.Empty ? null : settlementId;
    }

    public void LinkToDocument(Guid documentId) => DocumentId = documentId == Guid.Empty ? null : documentId;

    private Money Sum(IEnumerable<PayrollItem> items)
    {
        var list = items.ToList();
        return list.Count == 0
            ? Money.Zero(CurrencyCode)
            : list.Select(i => i.Amount).Aggregate((accumulated, next) => accumulated.Add(next));
    }

    private void EnsureDraft()
    {
        if (Status != PayrollStatus.Draft)
            throw new InvalidOperationException($"The payroll is {Status} and can no longer be edited.");
    }
}
