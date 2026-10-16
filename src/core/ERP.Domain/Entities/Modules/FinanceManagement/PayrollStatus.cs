namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Where a payslip has got to. Only a finalised one is owed to anybody.
/// </summary>
public enum PayrollStatus
{
    Draft = 0,
    Definite = 1,
    Paid = 2
}
