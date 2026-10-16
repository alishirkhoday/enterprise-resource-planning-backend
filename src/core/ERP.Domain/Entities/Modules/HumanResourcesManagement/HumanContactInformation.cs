namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// A way to reach somebody. Several per person, because the number payroll uses and the one
/// a colleague calls are often not the same.
/// </summary>
public sealed class HumanContactInformation : BaseEntity
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }
    public HumanContactType Type { get; private set; }
    public string Value { get; private set; } = default!;
    public bool IsDefault { get; private set; }

    private HumanContactInformation() { }

    public HumanContactInformation(Human human, HumanContactType type, string value, bool isDefault = false)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
        Human = human;
        HumanId = human.Id;
        Type = type;
        Value = value.Trim();
        IsDefault = isDefault;
    }

    public void ChangeValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
        Touch();
    }

    public void SetDefault()
    {
        IsDefault = true;
        Touch();
    }

    public void UnsetDefault()
    {
        IsDefault = false;
        Touch();
    }
}
