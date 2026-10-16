namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Whether a salary item adds to pay or takes away from it.
/// </summary>
public enum SalaryItemNature
{
    /// <summary>Adds to gross pay.</summary>
    Earning = 0,

    /// <summary>Subtracts from gross pay.</summary>
    Deduction = 1,

    /// <summary>Paid by the employer on top of pay, such as the employer insurance share.</summary>
    EmployerContribution = 2
}
