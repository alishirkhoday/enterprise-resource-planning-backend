namespace ERP.Domain.Entities.Common;

/// <summary>
/// A single configurable parameter of the installation, such as the default currency or the
/// number of days an invoice stays open. Values are stored as text and interpreted by <see cref="ValueType"/>.
/// </summary>
public sealed class SystemSetting : BaseEntity, IBranchScoped
{
    /// <summary>Dotted key, for example Finance.DefaultCurrency.</summary>
    public string Key { get; private set; } = default!;
    public string? Value { get; private set; }
    public SettingValueType ValueType { get; private set; }
    public string? Description { get; private set; }

    /// <summary>System settings are seeded by the product and must not be deleted by users.</summary>
    public bool IsSystematic { get; private set; }

    /// <summary>When true the value must never be written to logs or returned to clients in clear text.</summary>
    public bool IsSensitive { get; private set; }

    public Guid? BranchId { get; private set; }

    private SystemSetting() { }

    public SystemSetting(string key, string? value, SettingValueType valueType, bool isSystematic = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        Key = key.Trim();
        Value = value;
        ValueType = valueType;
        IsSystematic = isSystematic;
    }

    public void SetValue(string? value) => Value = value;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void MarkAsSensitive() => IsSensitive = true;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;
}
