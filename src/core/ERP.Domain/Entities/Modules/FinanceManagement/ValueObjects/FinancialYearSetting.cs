namespace ERP.Domain.Entities.Modules.FinanceManagement.ValueObjects;

/// <summary>
/// The figures a financial year fixes for everything inside it, starting with the statutory
/// base salary - حداقل حقوق. Payroll for a month is valued against the year's figure rather
/// than today's, so reopening an old month does not repay it at this year's rates.
/// </summary>
public class FinancialYearSetting : ValueObject
{
    public decimal BaseSalary { get; }

    private FinancialYearSetting()
    {
    }

    public FinancialYearSetting(decimal baseSalary)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseSalary, nameof(baseSalary));
        BaseSalary = baseSalary;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return BaseSalary;
    }
}
