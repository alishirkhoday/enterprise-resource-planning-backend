namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A move of an asset between custodians, cost centres, branches or locations. Recording it is
/// what makes a physical asset count reconcilable.
/// </summary>
public sealed class AssetTransfer : BaseEntity
{
    public FixedAsset Asset { get; private set; } = default!;
    public Guid AssetId { get; private set; }

    public DateOnly TransferDate { get; private set; }

    public Guid? FromCustodianHumanId { get; private set; }
    public Guid? ToCustodianHumanId { get; private set; }

    public Guid? FromCostCenterId { get; private set; }
    public Guid? ToCostCenterId { get; private set; }

    public Guid? FromBranchId { get; private set; }
    public Guid? ToBranchId { get; private set; }

    public string? FromLocation { get; private set; }
    public string? ToLocation { get; private set; }

    public string? Reason { get; private set; }
    public Guid? ApprovedByHumanId { get; private set; }

    private AssetTransfer() { }

    public AssetTransfer(FixedAsset asset, DateOnly transferDate)
    {
        ArgumentNullException.ThrowIfNull(asset, nameof(asset));
        if (asset.Status == FixedAssetStatus.Disposed)
            throw new InvalidOperationException("A disposed asset cannot be transferred.");

        Asset = asset;
        AssetId = asset.Id;
        TransferDate = transferDate;
        FromCustodianHumanId = asset.CustodianHumanId;
        FromCostCenterId = asset.CostCenterId;
        FromBranchId = asset.BranchId;
        FromLocation = asset.Location;
    }

    /// <summary>Applies the move to the asset and records where it went.</summary>
    public void TransferTo(Guid? custodianHumanId, Guid? costCenterId, Guid? branchId, string? location)
    {
        ToCustodianHumanId = custodianHumanId == Guid.Empty ? null : custodianHumanId;
        ToCostCenterId = costCenterId == Guid.Empty ? null : costCenterId;
        ToBranchId = branchId == Guid.Empty ? null : branchId;
        ToLocation = string.IsNullOrWhiteSpace(location) ? null : location.Trim();

        if (ToCustodianHumanId.HasValue)
            Asset.AssignCustodian(ToCustodianHumanId.Value);
        if (ToCostCenterId.HasValue)
            Asset.AssignCostCenter(ToCostCenterId.Value);
        if (ToBranchId.HasValue)
            Asset.AssignToBranch(ToBranchId.Value);
        if (ToLocation is not null)
            Asset.SetLocation(ToLocation);
    }

    public void SetReason(string? reason) => Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

    public void Approve(Guid humanId) => ApprovedByHumanId = humanId == Guid.Empty ? null : humanId;
}
