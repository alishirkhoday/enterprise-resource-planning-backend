namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// The running balance of one account, optionally for one subsidiary account, within one period.
/// It is derived data kept so a trial balance does not have to re-aggregate the whole journal.
/// </summary>
public sealed class AccountPeriodBalance : BaseEntity, IBranchScoped
{
    public Guid FinancialYearId { get; private set; }
    public Guid AccountingPeriodId { get; private set; }

    public Account Account { get; private set; } = default!;
    public Guid AccountId { get; private set; }

    public Guid? DetailAccount1Id { get; private set; }
    public Guid? DetailAccount2Id { get; private set; }
    public Guid? DetailAccount3Id { get; private set; }

    public Guid? BranchId { get; private set; }
    public string CurrencyCode { get; private set; } = default!;

    public decimal OpeningDebtor { get; private set; }
    public decimal OpeningCreditor { get; private set; }
    public decimal PeriodDebtor { get; private set; }
    public decimal PeriodCreditor { get; private set; }

    public decimal ClosingDebtor => OpeningDebtor + PeriodDebtor;
    public decimal ClosingCreditor => OpeningCreditor + PeriodCreditor;

    /// <summary>The closing balance expressed in the direction the account normally carries.</summary>
    public decimal NetBalance => Account.SignedAmount(ClosingDebtor, ClosingCreditor);

    private AccountPeriodBalance() { }

    public AccountPeriodBalance(Guid financialYearId, Guid accountingPeriodId, Account account, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(account, nameof(account));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        if (financialYearId == Guid.Empty)
            throw new ArgumentException("FinancialYearId is empty.", nameof(financialYearId));
        if (accountingPeriodId == Guid.Empty)
            throw new ArgumentException("AccountingPeriodId is empty.", nameof(accountingPeriodId));

        FinancialYearId = financialYearId;
        AccountingPeriodId = accountingPeriodId;
        Account = account;
        AccountId = account.Id;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
    }

    public void ScopeToDetailAccounts(Guid? level1, Guid? level2, Guid? level3)
    {
        DetailAccount1Id = level1 == Guid.Empty ? null : level1;
        DetailAccount2Id = level2 == Guid.Empty ? null : level2;
        DetailAccount3Id = level3 == Guid.Empty ? null : level3;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetOpening(decimal debtor, decimal creditor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(debtor, nameof(debtor));
        ArgumentOutOfRangeException.ThrowIfNegative(creditor, nameof(creditor));
        OpeningDebtor = debtor;
        OpeningCreditor = creditor;
    }

    public void Accumulate(decimal debtor, decimal creditor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(debtor, nameof(debtor));
        ArgumentOutOfRangeException.ThrowIfNegative(creditor, nameof(creditor));
        PeriodDebtor += debtor;
        PeriodCreditor += creditor;
    }

    public void Reset()
    {
        PeriodDebtor = 0m;
        PeriodCreditor = 0m;
    }
}
