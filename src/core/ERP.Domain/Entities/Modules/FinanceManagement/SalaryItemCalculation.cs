namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// How a salary item works out its amount - a fixed figure, a percentage of something, or a
/// rate per unit of time.
/// </summary>
public enum SalaryItemCalculation
{
    FixedAmount = 0,

    /// <summary>A share of the base wage.</summary>
    PercentageOfBase = 1,

    /// <summary>A share of another named item.</summary>
    PercentageOfItem = 2,

    /// <summary>A rate multiplied by the number of days worked.</summary>
    PerWorkedDay = 3,

    /// <summary>A rate multiplied by the number of hours worked.</summary>
    PerWorkedHour = 4,

    /// <summary>Computed by a rule the payroll run applies, such as the tax bracket table.</summary>
    Formula = 5
}
