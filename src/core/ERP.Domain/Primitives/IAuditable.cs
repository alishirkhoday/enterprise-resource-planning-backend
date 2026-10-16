namespace ERP.Domain.Primitives;

/// <summary>
/// Implemented by entities that record who created and last modified them.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreationDateTime { get; }
    string? CreatedBy { get; }
    DateTimeOffset? ModificationDateTime { get; }
    string? ModifiedBy { get; }

    void SetCreatedBy(string? createdBy);
    void Touch(string? modifiedBy = null);
}
