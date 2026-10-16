namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// One line of a journal entry. Exactly one of debit or credit carries a value, and the
/// subsidiary accounts are validated against what the ledger account declares.
/// </summary>
public sealed class DocumentItem : BaseEntity
{
    public Document Document { get; private set; } = default!;
    public Guid DocumentId { get; private set; }

    public Account Account { get; private set; } = default!;
    public Guid AccountId { get; private set; }

    public DetailAccount? DetailAccount1 { get; private set; }
    public Guid? DetailAccount1Id { get; private set; }

    public DetailAccount? DetailAccount2 { get; private set; }
    public Guid? DetailAccount2Id { get; private set; }

    public DetailAccount? DetailAccount3 { get; private set; }
    public Guid? DetailAccount3Id { get; private set; }

    public short Priority { get; private set; }
    public decimal Debtor { get; private set; }
    public decimal Creditor { get; private set; }
    public string CurrencyCode { get; private set; } = default!;

    /// <summary>The absolute value of the line, whichever side it falls on.</summary>
    public Money Amount => Money.Create(Debtor > 0 ? Debtor : Creditor, CurrencyCode);

    /// <summary>The original amount when the line was entered in a foreign currency.</summary>
    public Money? ForeignAmount { get; private set; }
    public decimal? ExchangeRate { get; private set; }

    public Guid? CostCenterId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string? Description { get; private set; }

    private DocumentItem() { }

    public DocumentItem(Document document, Account account, short priority, decimal debtor, decimal creditor)
    {
        ArgumentNullException.ThrowIfNull(document, nameof(document));
        ArgumentNullException.ThrowIfNull(account, nameof(account));
        ArgumentOutOfRangeException.ThrowIfNegative(debtor, nameof(debtor));
        ArgumentOutOfRangeException.ThrowIfNegative(creditor, nameof(creditor));
        if (debtor <= 0 && creditor <= 0 || debtor > 0 && creditor > 0)
            throw new ArgumentException("Either debtor or creditor must be set, but not both.");

        Document = document;
        DocumentId = document.Id;
        Account = account;
        AccountId = account.Id;
        Priority = priority;
        Debtor = debtor;
        Creditor = creditor;
        CurrencyCode = document.CurrencyCode;
    }

    /// <summary>
    /// Attaches the subsidiary accounts this line belongs to, after checking them against the
    /// groups the ledger account declares.
    /// </summary>
    public void SetDetailAccounts(DetailAccount? level1, DetailAccount? level2 = null, DetailAccount? level3 = null)
    {
        Account.EnsureDetailAccountsAreValid(level1, level2, level3);
        DetailAccount1 = level1;
        DetailAccount1Id = level1?.Id;
        DetailAccount2 = level2;
        DetailAccount2Id = level2?.Id;
        DetailAccount3 = level3;
        DetailAccount3Id = level3?.Id;
    }

    /// <summary>Records the foreign currency original of a line that was converted to book currency.</summary>
    public void SetForeignAmount(Money foreignAmount, decimal exchangeRate)
    {
        ArgumentNullException.ThrowIfNull(foreignAmount, nameof(foreignAmount));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exchangeRate, nameof(exchangeRate));
        if (foreignAmount.CurrencyCode == CurrencyCode)
            throw new ArgumentException("The foreign amount must be in a different currency than the entry.", nameof(foreignAmount));
        ForeignAmount = foreignAmount;
        ExchangeRate = exchangeRate;
    }

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void AssignProject(Guid projectId) => ProjectId = projectId == Guid.Empty ? null : projectId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public bool IsDebit => Debtor > 0;
}
