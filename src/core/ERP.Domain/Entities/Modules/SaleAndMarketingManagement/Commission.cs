namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// Commission earned by a salesperson on one sale, from the moment it is accrued until payroll
/// actually pays it.
/// </summary>
public sealed class Commission : BaseEntity
{
    public Guid SalesRepHumanId { get; private set; }

    /// <summary>What the commission was earned on, for example Invoice together with its identifier.</summary>
    public string ReferenceType { get; private set; } = default!;
    public string ReferenceId { get; private set; } = default!;

    public DateOnly EarnedOn { get; private set; }

    /// <summary>The sale value the commission is computed from.</summary>
    public Money BaseAmount { get; private set; } = default!;

    public Percentage Rate { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;

    public CommissionStatus Status { get; private set; }
    public Guid? PayrollId { get; private set; }
    public DateOnly? PaidOn { get; private set; }
    public string? Description { get; private set; }

    private Commission() { }

    public Commission(Guid salesRepHumanId, string referenceType, string referenceId, DateOnly earnedOn, Money baseAmount, Percentage rate)
    {
        ArgumentNullException.ThrowIfNull(baseAmount, nameof(baseAmount));
        ArgumentNullException.ThrowIfNull(rate, nameof(rate));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        if (salesRepHumanId == Guid.Empty)
            throw new ArgumentException("SalesRepHumanId is empty.", nameof(salesRepHumanId));
        if (baseAmount.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(baseAmount), "A base amount cannot be negative.");

        SalesRepHumanId = salesRepHumanId;
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
        EarnedOn = earnedOn;
        BaseAmount = baseAmount;
        Rate = rate;
        Amount = baseAmount.Percent(rate);
        Status = CommissionStatus.Accrued;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>
    /// Confirms the commission is really owed, which normally waits until the customer has paid.
    /// </summary>
    public void Approve()
    {
        if (Status != CommissionStatus.Accrued)
            throw new InvalidOperationException($"Only an accrued commission can be approved; this one is {Status}.");
        Status = CommissionStatus.Approved;
    }

    public void MarkAsPaid(DateOnly paidOn, Guid? payrollId = null)
    {
        if (Status != CommissionStatus.Approved)
            throw new InvalidOperationException("Only an approved commission can be paid.");
        PaidOn = paidOn;
        PayrollId = payrollId == Guid.Empty ? null : payrollId;
        Status = CommissionStatus.Paid;
    }

    /// <summary>Takes back a commission whose underlying sale was reversed.</summary>
    public void Reverse(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        if (Status == CommissionStatus.Paid)
            throw new InvalidOperationException("A paid commission must be clawed back through payroll, not reversed here.");
        Status = CommissionStatus.Reversed;
        Description = reason.Trim();
    }
}
