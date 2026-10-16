namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Where a tax return has got to on its way to the authority - سامانه مؤدیان.
/// </summary>
public enum TaxReturnStatus
{
    Draft = 0,
    Submitted = 1,
    Paid = 2
}
