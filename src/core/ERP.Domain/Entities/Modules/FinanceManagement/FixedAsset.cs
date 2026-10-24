namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Something the organization owns and uses over several years. Its cost is spread across those
/// years through depreciation entries rather than being expensed when it is bought.
/// </summary>
public sealed partial class FixedAsset : AggregateRoot, IBranchScoped
{
    public AssetCategory Category { get; private set; } = default!;
    public Guid CategoryId { get; private set; }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? SerialNumber { get; private set; }
    public string? TagNumber { get; private set; }

    public FixedAssetStatus Status { get; private set; }

    public DateOnly AcquisitionDate { get; private set; }

    /// <summary>What the asset cost, including anything spent to bring it into use.</summary>
    public Money AcquisitionCost { get; private set; } = default!;

    /// <summary>What it is expected to be worth at the end of its life.</summary>
    public Money SalvageValue { get; private set; } = default!;

    /// <summary>Depreciation charged so far.</summary>
    public Money AccumulatedDepreciation { get; private set; } = default!;

    public Money BookValue => AcquisitionCost.Subtract(AccumulatedDepreciation);

    /// <summary>The part of the cost that is spread over the life.</summary>
    public Money DepreciableAmount => AcquisitionCost.Subtract(SalvageValue);

    public DepreciationMethod DepreciationMethod { get; private set; }
    public int UsefulLifeMonths { get; private set; }
    public DateOnly? DepreciationStartDate { get; private set; }

    /// <summary>Expected total output, for an asset depreciated by units of production.</summary>
    public decimal? TotalExpectedUnits { get; private set; }
    public decimal ProducedUnits { get; private set; }

    public Guid? CostCenterId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? CustodianHumanId { get; private set; }
    public string? Location { get; private set; }

    public DateOnly? DisposalDate { get; private set; }
    public string? Description { get; private set; }

    private readonly List<DepreciationEntry> _depreciationEntries = [];
    public IReadOnlyList<DepreciationEntry> DepreciationEntries => _depreciationEntries.AsReadOnly();

    private FixedAsset() { }

    public FixedAsset(AssetCategory category, string code, string name, DateOnly acquisitionDate, Money acquisitionCost)
    {
        ArgumentNullException.ThrowIfNull(category, nameof(category));
        ArgumentNullException.ThrowIfNull(acquisitionCost, nameof(acquisitionCost));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        if (!acquisitionCost.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(acquisitionCost), "An asset must have a positive cost.");

        Category = category;
        CategoryId = category.Id;
        Code = code.Trim();
        Name = name.Trim();
        AcquisitionDate = acquisitionDate;
        AcquisitionCost = acquisitionCost;
        SalvageValue = acquisitionCost.Percent(category.DefaultSalvagePercentage);
        AccumulatedDepreciation = Money.Zero(acquisitionCost.CurrencyCode);
        DepreciationMethod = category.DefaultDepreciationMethod;
        UsefulLifeMonths = category.DefaultUsefulLifeMonths;
        Status = FixedAssetStatus.Acquired;
    }

    public void SetIdentifiers(string? serialNumber, string? tagNumber)
    {
        SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim();
        TagNumber = string.IsNullOrWhiteSpace(tagNumber) ? null : tagNumber.Trim();
    }

    public void SetDepreciationPolicy(DepreciationMethod method, int usefulLifeMonths, Money salvageValue)
    {
        ArgumentNullException.ThrowIfNull(salvageValue, nameof(salvageValue));
        if (Status == FixedAssetStatus.Disposed)
            throw new InvalidOperationException("A disposed asset cannot change its depreciation policy.");
        if (method != DepreciationMethod.None)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(usefulLifeMonths, nameof(usefulLifeMonths));
        if (salvageValue.CurrencyCode != AcquisitionCost.CurrencyCode)
            throw new ArgumentException("The salvage value must share the asset currency.", nameof(salvageValue));
        if (salvageValue.Amount > AcquisitionCost.Amount)
            throw new ArgumentOutOfRangeException(nameof(salvageValue), "The salvage value cannot exceed the cost.");

        DepreciationMethod = method;
        UsefulLifeMonths = usefulLifeMonths;
        SalvageValue = salvageValue;
    }

    public void SetExpectedUnits(decimal totalExpectedUnits)
    {
        if (DepreciationMethod != DepreciationMethod.UnitsOfProduction)
            throw new InvalidOperationException("Expected units only apply to units of production depreciation.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalExpectedUnits, nameof(totalExpectedUnits));
        TotalExpectedUnits = totalExpectedUnits;
    }

    public void PlaceInService(DateOnly depreciationStartDate)
    {
        if (Status != FixedAssetStatus.Acquired)
            throw new InvalidOperationException($"Only an acquired asset can be placed in service; this one is {Status}.");
        if (depreciationStartDate < AcquisitionDate)
            throw new ArgumentOutOfRangeException(nameof(depreciationStartDate), "Depreciation cannot start before acquisition.");
        DepreciationStartDate = depreciationStartDate;
        Status = FixedAssetStatus.InService;
    }

    public void AssignCustodian(Guid humanId) => CustodianHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignCostCenter(Guid costCenterId) => CostCenterId = costCenterId == Guid.Empty ? null : costCenterId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetLocation(string? location) => Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>Adds a cost that improves the asset rather than merely maintaining it.</summary>
    public void Capitalize(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount, nameof(amount));
        if (Status == FixedAssetStatus.Disposed)
            throw new InvalidOperationException("A disposed asset cannot be improved.");
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "A capitalised amount must be positive.");
        AcquisitionCost = AcquisitionCost.Add(amount);
    }

    public void RecordProduction(decimal units)
    {
        if (DepreciationMethod != DepreciationMethod.UnitsOfProduction)
            throw new InvalidOperationException("Production is only tracked for units of production depreciation.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(units, nameof(units));
        ProducedUnits += units;
    }

    /// <summary>
    /// The charge for one period under the asset policy, never taking the book value below salvage.
    /// </summary>
    public Money CalculateDepreciationForPeriod(decimal unitsProducedInPeriod = 0m)
    {
        if (DepreciationMethod == DepreciationMethod.None || Status != FixedAssetStatus.InService)
            return Money.Zero(AcquisitionCost.CurrencyCode);

        var remaining = BookValue.Subtract(SalvageValue);
        if (!remaining.IsPositive)
            return Money.Zero(AcquisitionCost.CurrencyCode);

        var charge = DepreciationMethod switch
        {
            DepreciationMethod.StraightLine => DepreciableAmount.Divide(UsefulLifeMonths),
            DepreciationMethod.DecliningBalance => BookValue.Percent(Category.DecliningBalanceRate ?? Percentage.Zero()).Divide(12m),
            DepreciationMethod.UnitsOfProduction => TotalExpectedUnits is null or 0m
                ? Money.Zero(AcquisitionCost.CurrencyCode)
                : DepreciableAmount.Multiply(unitsProducedInPeriod / TotalExpectedUnits.Value),
            _ => Money.Zero(AcquisitionCost.CurrencyCode)
        };

        return charge.Amount > remaining.Amount ? remaining : charge;
    }

    public void AddDepreciationEntry(DepreciationEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        EnsureCanAddChild(entry, entry.AssetId, _depreciationEntries);

        if (Status != FixedAssetStatus.InService)
            throw new InvalidOperationException($"Depreciation can only be charged while the asset is in service; this one is {Status}.");
        if (_depreciationEntries.Any(e => e.Period.Overlaps(entry.Period)))
            throw new InvalidOperationException("Depreciation has already been charged for an overlapping period.");

        var accumulatedDepreciation = AccumulatedDepreciation.Add(entry.Amount);
        _depreciationEntries.Add(entry);
        AccumulatedDepreciation = accumulatedDepreciation;

        if (BookValue.Amount <= SalvageValue.Amount)
            Status = FixedAssetStatus.FullyDepreciated;
    }

    public void Retire(DateOnly disposalDate, string? reason = null)
    {
        if (Status == FixedAssetStatus.Disposed)
            throw new InvalidOperationException("The asset is already disposed.");
        if (disposalDate < AcquisitionDate)
            throw new ArgumentOutOfRangeException(nameof(disposalDate), "Disposal cannot precede acquisition.");
        DisposalDate = disposalDate;
        Status = FixedAssetStatus.Disposed;
        if (!string.IsNullOrWhiteSpace(reason))
            Description = reason.Trim();
    }

    public void MarkAsUnderMaintenance()
    {
        if (Status != FixedAssetStatus.InService)
            throw new InvalidOperationException("Only an in service asset can go under maintenance.");
        Status = FixedAssetStatus.UnderMaintenance;
    }

    public void ReturnToService()
    {
        if (Status != FixedAssetStatus.UnderMaintenance)
            throw new InvalidOperationException("Only an asset under maintenance can return to service.");
        Status = FixedAssetStatus.InService;
    }
}
