using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

public sealed partial class ServiceContract
{
    public void SetServiceContractCoverageLocation(Guid childId, Address location)
    {
        EnsureDraft();
        var child = GetChild(childId, _coveredAssets);
        child.SetLocation(location);
    }

    public void SetServiceContractCoverageInstalledOn(Guid childId, DateOnly installedOn)
    {
        EnsureDraft();
        var child = GetChild(childId, _coveredAssets);
        child.SetInstalledOn(installedOn);
    }

    public void SetServiceContractCoverageDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _coveredAssets);
        child.SetDescription(description);
    }
}
