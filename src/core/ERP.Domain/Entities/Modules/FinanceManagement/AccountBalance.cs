namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Which side an account is expected to sit on - بدهکار یا بستانکار. Both is for accounts
/// that legitimately swing either way, such as a party account.
/// </summary>
public enum AccountBalance
{
    /// <summary>
    /// بدهکار
    /// </summary>
    Debtor = 0,
    /// <summary>
    /// بستانکار
    /// </summary>
    Creditor = 1,
    /// <summary>
    /// خنثی
    /// </summary>
    Both = 2
}
