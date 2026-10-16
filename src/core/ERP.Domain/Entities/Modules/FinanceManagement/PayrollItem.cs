namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// One line of a pay slip. Keeping the nature and the tax treatment on the line is what lets the
/// slip be recomputed and audited without re-reading the structure it came from.
/// </summary>
public sealed class PayrollItem : BaseEntity
{
    public Payroll Payroll { get; private set; } = default!;
    public Guid PayrollId { get; private set; }

    public string Code { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public SalaryItemNature Nature { get; private set; }
    public Money Amount { get; private set; } = default!;

    public bool IsTaxable { get; private set; }
    public bool IsInsurable { get; private set; }

    /// <summary>The quantity the amount was computed from, such as overtime hours.</summary>
    public decimal? Quantity { get; private set; }
    public Money? UnitRate { get; private set; }

    public Guid? SalaryStructureItemId { get; private set; }
    public Guid? AccountId { get; private set; }
    public string? Description { get; private set; }

    private PayrollItem() { }

    public PayrollItem(Payroll payroll, string code, string title, SalaryItemNature nature, Money amount)
    {
        ArgumentNullException.ThrowIfNull(payroll, nameof(payroll));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        if (amount.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(amount), "A payroll line carries a positive amount; the nature decides its sign.");

        Payroll = payroll;
        PayrollId = payroll.Id;
        Code = code.Trim();
        Title = title.Trim();
        Nature = nature;
        Amount = amount;
    }

    public void SetTaxTreatment(bool isTaxable, bool isInsurable)
    {
        IsTaxable = isTaxable;
        IsInsurable = isInsurable;
    }

    public void SetQuantity(decimal quantity, Money unitRate)
    {
        ArgumentNullException.ThrowIfNull(unitRate, nameof(unitRate));
        ArgumentOutOfRangeException.ThrowIfNegative(quantity, nameof(quantity));
        Quantity = quantity;
        UnitRate = unitRate;
    }

    public void LinkToStructureItem(Guid salaryStructureItemId) =>
        SalaryStructureItemId = salaryStructureItemId == Guid.Empty ? null : salaryStructureItemId;

    public void LinkAccount(Guid accountId) => AccountId = accountId == Guid.Empty ? null : accountId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
