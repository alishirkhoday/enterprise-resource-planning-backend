namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Where an asset is in its life. Only one in service depreciates, which is what keeps a
/// fully depreciated or disposed asset out of a monthly run.
/// </summary>
public enum FixedAssetStatus
{
    /// <summary>Bought but not yet in use, so not yet depreciating.</summary>
    Acquired = 0,

    InService = 1,
    UnderMaintenance = 2,

    /// <summary>Written down to its salvage value; no further depreciation is charged.</summary>
    FullyDepreciated = 3,

    Disposed = 4
}
