namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Which statement an account ends up on: the balance sheet, or the income and expense
/// statement. Assets, liabilities and equity carry forward; revenues and expenses do not.
/// </summary>
public enum FinancialStatement
{
    /// <summary>
    /// ترازنامه ای
    /// </summary>
    BalanceSheet = 0,
    /// <summary>
    /// صورت درآمد و هزینه
    /// </summary>
    IncomeAndExpenseStatement = 1
}
