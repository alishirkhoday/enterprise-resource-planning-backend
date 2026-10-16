using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

/// <summary>
/// One thing a <see cref="ServiceContract"/> covers. Naming a serial number covers that single
/// unit; leaving it empty covers every unit of the item the customer holds, which is how fleet and
/// site-wide agreements are written.
/// </summary>
public sealed class ServiceContractCoverage : BaseEntity
{
    public ServiceContract Contract { get; private set; } = default!;
    public Guid ContractId { get; private set; }

    public Item Item { get; private set; } = default!;
    public Guid ItemId { get; private set; }

    /// <summary>The single unit covered, or null when the whole item is.</summary>
    public Guid? SerialNumberId { get; private set; }

    /// <summary>Where the covered equipment lives, for routing an engineer to it.</summary>
    public Address? Location { get; private set; }

    public DateOnly? InstalledOn { get; private set; }
    public string? Description { get; private set; }

    private ServiceContractCoverage() { }

    public ServiceContractCoverage(ServiceContract contract, Item item, Guid? serialNumberId = null)
    {
        ArgumentNullException.ThrowIfNull(contract, nameof(contract));
        ArgumentNullException.ThrowIfNull(item, nameof(item));

        Contract = contract;
        ContractId = contract.Id;
        Item = item;
        ItemId = item.Id;
        SerialNumberId = serialNumberId == Guid.Empty ? null : serialNumberId;
    }

    public void SetLocation(Address location)
    {
        ArgumentNullException.ThrowIfNull(location, nameof(location));
        Location = location;
    }

    public void SetInstalledOn(DateOnly installedOn) => InstalledOn = installedOn;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public bool IsSpecificUnit => SerialNumberId.HasValue;
}
