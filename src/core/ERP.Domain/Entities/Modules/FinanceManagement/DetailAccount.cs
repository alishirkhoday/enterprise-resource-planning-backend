namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// A subsidiary account. It answers whose balance a ledger line belongs to, without multiplying
/// the chart of accounts by one account per customer.
/// </summary>
public sealed class DetailAccount : AggregateRoot
{
    public DetailAccountGroup Group { get; private set; } = default!;
    public Guid GroupId { get; private set; }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    /// <summary>Identifier of the record this subsidiary account stands for, when it stands for one.</summary>
    public string? ReferenceId { get; private set; }

    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private DetailAccount() { }

    public DetailAccount(DetailAccountGroup group, string code, string name, string? referenceId = null)
    {
        ArgumentNullException.ThrowIfNull(group, nameof(group));
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Group = group;
        GroupId = group.Id;
        Code = code.Trim();
        Name = name.Trim();
        ReferenceId = string.IsNullOrWhiteSpace(referenceId) ? null : referenceId.Trim();
        IsActive = true;
    }

    public void Update(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
    }

    public void LinkTo(string referenceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        ReferenceId = referenceId.Trim();
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
