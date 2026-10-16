namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Where somebody lives. Held as history rather than a single field, since an old address
/// still explains an old payslip.
/// </summary>
public sealed class HumanAddress : BaseEntity
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public string Title { get; private set; } = default!;
    public Address Address { get; private set; } = default!;
    public bool IsDefault { get; private set; }

    private HumanAddress() { }

    public HumanAddress(Human human, string title, Address address)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        Human = human;
        HumanId = human.Id;
        Title = title.Trim();
        Address = address;
    }

    public void Update(string title, Address address)
    {
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        Title = title.Trim();
        Address = address;
    }

    public void SetDefault() => IsDefault = true;
    public void UnsetDefault() => IsDefault = false;
}
