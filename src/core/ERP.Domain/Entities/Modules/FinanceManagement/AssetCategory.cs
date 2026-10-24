namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A class of fixed assets that share a depreciation policy and the same ledger accounts.
/// </summary>
public sealed class AssetCategory : AggregateRoot
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    public DepreciationMethod DefaultDepreciationMethod { get; private set; }

    /// <summary>Expected life in months, which sets the default depreciation schedule.</summary>
    public int DefaultUsefulLifeMonths { get; private set; }

    /// <summary>The proportion of cost expected to remain at the end of the life.</summary>
    public Percentage DefaultSalvagePercentage { get; private set; } = default!;

    /// <summary>Annual rate used by the declining balance method.</summary>
    public Percentage? DecliningBalanceRate { get; private set; }

    public Guid? AssetAccountId { get; private set; }
    public Guid? AccumulatedDepreciationAccountId { get; private set; }
    public Guid? DepreciationExpenseAccountId { get; private set; }
    public Guid? DisposalGainLossAccountId { get; private set; }

    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private AssetCategory() { }

    public AssetCategory(string code, string name, DepreciationMethod defaultDepreciationMethod, int defaultUsefulLifeMonths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(defaultUsefulLifeMonths, nameof(defaultUsefulLifeMonths));

        Code = code.Trim();
        Name = name.Trim();
        DefaultDepreciationMethod = defaultDepreciationMethod;
        DefaultUsefulLifeMonths = defaultUsefulLifeMonths;
        DefaultSalvagePercentage = Percentage.Zero();
        IsActive = true;
    }

    public void Update(string name, DepreciationMethod method, int usefulLifeMonths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(usefulLifeMonths, nameof(usefulLifeMonths));
        Name = name.Trim();
        DefaultDepreciationMethod = method;
        DefaultUsefulLifeMonths = usefulLifeMonths;
    }

    public void SetSalvagePercentage(Percentage salvagePercentage)
    {
        ArgumentNullException.ThrowIfNull(salvagePercentage, nameof(salvagePercentage));
        DefaultSalvagePercentage = salvagePercentage;
    }

    public void SetDecliningBalanceRate(Percentage rate)
    {
        ArgumentNullException.ThrowIfNull(rate, nameof(rate));
        DecliningBalanceRate = rate;
    }

    public void LinkAccounts(Guid? assetAccountId, Guid? accumulatedDepreciationAccountId, Guid? depreciationExpenseAccountId, Guid? disposalGainLossAccountId)
    {
        AssetAccountId = assetAccountId == Guid.Empty ? null : assetAccountId;
        AccumulatedDepreciationAccountId = accumulatedDepreciationAccountId == Guid.Empty ? null : accumulatedDepreciationAccountId;
        DepreciationExpenseAccountId = depreciationExpenseAccountId == Guid.Empty ? null : depreciationExpenseAccountId;
        DisposalGainLossAccountId = disposalGainLossAccountId == Guid.Empty ? null : disposalGainLossAccountId;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
