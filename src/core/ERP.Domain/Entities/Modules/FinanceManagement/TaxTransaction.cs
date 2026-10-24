namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// The tax carried by one business document, recorded separately from the document so a tax
/// return can be assembled without re-reading every invoice.
/// </summary>
public sealed class TaxTransaction : AggregateRoot, IBranchScoped
{
    public Tax Tax { get; private set; } = default!;
    public Guid TaxId { get; private set; }

    public TaxDirection Direction { get; private set; }
    public DateOnly TransactionDate { get; private set; }

    /// <summary>The amount the tax was calculated on.</summary>
    public Money TaxableAmount { get; private set; } = default!;

    public Money TaxAmount { get; private set; } = default!;
    public Percentage AppliedRate { get; private set; } = default!;

    public string ReferenceType { get; private set; } = default!;
    public string ReferenceId { get; private set; } = default!;
    public string? DocumentNumber { get; private set; }

    /// <summary>The subsidiary account of the counterparty, needed for the tax authority filing.</summary>
    public Guid? PartyDetailAccountId { get; private set; }
    public string? PartyEconomicCode { get; private set; }
    public string? PartyName { get; private set; }

    public Guid? DocumentId { get; private set; }
    public Guid? TaxReturnId { get; private set; }
    public Guid? BranchId { get; private set; }

    private TaxTransaction() { }

    public TaxTransaction(Tax tax, TaxDirection direction, DateOnly transactionDate, Money taxableAmount, string referenceType, string referenceId)
    {
        ArgumentNullException.ThrowIfNull(tax, nameof(tax));
        ArgumentNullException.ThrowIfNull(taxableAmount, nameof(taxableAmount));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        if (!tax.IsEffectiveOn(transactionDate))
            throw new InvalidOperationException($"Tax {tax.Code} is not effective on {transactionDate:yyyy/MM/dd}.");

        Tax = tax;
        TaxId = tax.Id;
        Direction = direction;
        TransactionDate = transactionDate;
        TaxableAmount = taxableAmount;
        TaxAmount = tax.CalculateOn(taxableAmount);
        AppliedRate = tax.Rate;
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
    }

    public void SetParty(Guid? partyDetailAccountId, string? partyName, string? partyEconomicCode)
    {
        PartyDetailAccountId = partyDetailAccountId == Guid.Empty ? null : partyDetailAccountId;
        PartyName = string.IsNullOrWhiteSpace(partyName) ? null : partyName.Trim();
        PartyEconomicCode = string.IsNullOrWhiteSpace(partyEconomicCode) ? null : partyEconomicCode.Trim();
    }

    public void SetDocumentNumber(string? documentNumber) =>
        DocumentNumber = string.IsNullOrWhiteSpace(documentNumber) ? null : documentNumber.Trim();

    public void LinkToDocument(Guid documentId) => DocumentId = documentId == Guid.Empty ? null : documentId;

    public void IncludeInReturn(Guid taxReturnId) => TaxReturnId = taxReturnId == Guid.Empty ? null : taxReturnId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;
}
