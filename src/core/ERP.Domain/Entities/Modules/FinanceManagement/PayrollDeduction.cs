namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Something taken off somebody's pay for a period - کسورات: tax, insurance, a loan
/// instalment, an advance.
/// </summary>
public sealed class PayrollDeduction : BaseEntity
{
    public Payroll Payroll { get; private set; } = default!;
    public Guid PayrollId { get; private set; }

    public Deduction Deduction { get; private set; } = default!;
    public Guid DeductionId { get; private set; }

    /// <summary>The amount actually withheld, which may differ from the deduction default.</summary>
    public Money AppliedAmount { get; private set; } = default!;

    public string? Description { get; private set; }

    private PayrollDeduction() { }

    public PayrollDeduction(Payroll payroll, Deduction deduction, Money appliedAmount)
    {
        ArgumentNullException.ThrowIfNull(payroll, nameof(payroll));
        ArgumentNullException.ThrowIfNull(deduction, nameof(deduction));
        ArgumentNullException.ThrowIfNull(appliedAmount, nameof(appliedAmount));
        if (appliedAmount.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(appliedAmount), "A withheld amount cannot be negative.");
        if (appliedAmount.CurrencyCode != payroll.CurrencyCode)
            throw new ArgumentException($"The deduction must be expressed in {payroll.CurrencyCode}.", nameof(appliedAmount));

        Payroll = payroll;
        PayrollId = payroll.Id;
        Deduction = deduction;
        DeductionId = deduction.Id;
        AppliedAmount = appliedAmount;
    }

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
