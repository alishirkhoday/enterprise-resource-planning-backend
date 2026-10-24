namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// The removal of an asset from the books, and the gain or loss it produced.
/// </summary>
public sealed class AssetDisposal : AggregateRoot
{
    public FixedAsset Asset { get; private set; } = default!;
    public Guid AssetId { get; private set; }

    public AssetDisposalReason Reason { get; private set; }
    public DateOnly DisposalDate { get; private set; }

    /// <summary>What the asset was sold for, or zero when it was scrapped.</summary>
    public Money Proceeds { get; private set; } = default!;

    /// <summary>The book value at the moment of disposal.</summary>
    public Money BookValueAtDisposal { get; private set; } = default!;

    /// <summary>Positive for a gain, negative for a loss.</summary>
    public Money GainOrLoss => Proceeds.Subtract(BookValueAtDisposal);

    /// <summary>The subsidiary account of the buyer, when the asset was sold.</summary>
    public Guid? BuyerDetailAccountId { get; private set; }

    public Guid? DocumentId { get; private set; }
    public string? Description { get; private set; }

    private AssetDisposal() { }

    public AssetDisposal(FixedAsset asset, AssetDisposalReason reason, DateOnly disposalDate, Money proceeds)
    {
        ArgumentNullException.ThrowIfNull(asset, nameof(asset));
        ArgumentNullException.ThrowIfNull(proceeds, nameof(proceeds));
        if (proceeds.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(proceeds), "Proceeds cannot be negative.");
        if (proceeds.CurrencyCode != asset.AcquisitionCost.CurrencyCode)
            throw new ArgumentException("The proceeds must share the asset currency.", nameof(proceeds));

        Asset = asset;
        AssetId = asset.Id;
        Reason = reason;
        DisposalDate = disposalDate;
        Proceeds = proceeds;
        BookValueAtDisposal = asset.BookValue;
    }

    public void SetBuyer(Guid buyerDetailAccountId) => BuyerDetailAccountId = buyerDetailAccountId == Guid.Empty ? null : buyerDetailAccountId;

    public void LinkToDocument(Guid documentId) => DocumentId = documentId == Guid.Empty ? null : documentId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public bool IsGain => GainOrLoss.IsPositive;
}
