namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// What kind of bank account it is, which decides what it may be used for.
/// </summary>
public enum BankAccountType
{
    Current = 0,
    Savings = 1,
    ShortTermDeposit = 2,
    LongTermDeposit = 3,
    Loan = 4,
    ForeignCurrency = 5
}
