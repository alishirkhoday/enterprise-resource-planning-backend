namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// The depreciation charged on one asset for one period, together with the journal entry it produced.
/// </summary>
public sealed class DepreciationEntry : BaseEntity
{
    public FixedAsset Asset { get; private set; } = default!;
    public Guid AssetId { get; private set; }

    public DateRange Period { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;

    /// <summary>The accumulated depreciation after this charge.</summary>
    public Money AccumulatedAfter { get; private set; } = default!;

    /// <summary>The book value after this charge.</summary>
    public Money BookValueAfter { get; private set; } = default!;

    public Guid? DocumentId { get; private set; }
    public Guid? AccountingPeriodId { get; private set; }
    public decimal? UnitsProduced { get; private set; }

    private DepreciationEntry() { }

    public DepreciationEntry(FixedAsset asset, DateRange period, Money amount)
    {
        ArgumentNullException.ThrowIfNull(asset, nameof(asset));
        ArgumentNullException.ThrowIfNull(period, nameof(period));
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (amount.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(amount), "A depreciation charge cannot be negative.");
        if (amount.CurrencyCode != asset.AcquisitionCost.CurrencyCode)
            throw new ArgumentException("The charge must share the asset currency.", nameof(amount));

        Asset = asset;
        AssetId = asset.Id;
        Period = period;
        Amount = amount;
        AccumulatedAfter = asset.AccumulatedDepreciation.Add(amount);
        BookValueAfter = asset.AcquisitionCost.Subtract(AccumulatedAfter);
    }

    public void SetUnitsProduced(decimal units)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(units, nameof(units));
        UnitsProduced = units;
    }

    public void LinkToDocument(Guid documentId) => DocumentId = documentId == Guid.Empty ? null : documentId;

    public void LinkToAccountingPeriod(Guid accountingPeriodId) => AccountingPeriodId = accountingPeriodId == Guid.Empty ? null : accountingPeriodId;
}
