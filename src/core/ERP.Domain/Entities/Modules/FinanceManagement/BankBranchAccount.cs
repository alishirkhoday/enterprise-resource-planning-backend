namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// An account the business holds at a bank branch - حساب بانکی.
/// </summary>
public sealed partial class BankBranchAccount : AggregateRoot, IBranchScoped
{
    public BankBranch BankBranch { get; private set; } = default!;
    public Guid BankBranchId { get; private set; }

    public string AccountNumber { get; private set; } = default!;
    public string? CardNumber { get; private set; }
    public string? InternationalBankAccountNumber { get; private set; }

    public string? Title { get; private set; }
    public BankAccountType Type { get; private set; }
    public string CurrencyCode { get; private set; } = default!;

    /// <summary>The ledger account this bank account reports into.</summary>
    public Guid? AccountId { get; private set; }
    public Guid? DetailAccountId { get; private set; }
    public Guid? BranchId { get; private set; }

    public DateOnly? OpenedOn { get; private set; }
    public DateOnly? ClosedOn { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<BankTransaction> _transactions = [];
    public IReadOnlyList<BankTransaction> Transactions => _transactions.AsReadOnly();

    private BankBranchAccount() { }

    public BankBranchAccount(BankBranch bankBranch, string accountNumber, string currencyCode = "IRR", BankAccountType type = BankAccountType.Current)
    {
        ArgumentNullException.ThrowIfNull(bankBranch, nameof(bankBranch));
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber, nameof(accountNumber));
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode, nameof(currencyCode));

        BankBranch = bankBranch;
        BankBranchId = bankBranch.Id;
        AccountNumber = accountNumber.Trim();
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        Type = type;
        IsActive = true;
    }

    public void SetIdentifiers(string? cardNumber, string? internationalBankAccountNumber)
    {
        CardNumber = string.IsNullOrWhiteSpace(cardNumber) ? null : cardNumber.Trim();
        InternationalBankAccountNumber = string.IsNullOrWhiteSpace(internationalBankAccountNumber)
            ? null
            : internationalBankAccountNumber.Trim().ToUpperInvariant();
    }

    public void SetTitle(string? title) => Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();

    public void LinkToLedger(Guid accountId, Guid? detailAccountId = null)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId is empty.", nameof(accountId));
        AccountId = accountId;
        DetailAccountId = detailAccountId == Guid.Empty ? null : detailAccountId;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetOpenedOn(DateOnly openedOn) => OpenedOn = openedOn;

    public void AddTransaction(BankTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        EnsureCanAddChild(transaction, transaction.BankBranchAccountId, _transactions);

        if (!IsActive)
            throw new InvalidOperationException($"Bank account {AccountNumber} is closed.");
        _transactions.Add(transaction);
    }

    public void Close(DateOnly closedOn)
    {
        ClosedOn = closedOn;
        IsActive = false;
    }
}
