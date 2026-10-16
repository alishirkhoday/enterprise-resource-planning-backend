namespace ERP.Domain.Primitives;

/// <summary>
/// Implemented by entities that are never physically removed, so financial history keeps its references.
/// Infrastructure is expected to apply a global query filter on <see cref="IsDeleted"/>.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    string? DeletedBy { get; }
    DateTimeOffset? DeletionDateTime { get; }

    void Delete(string? deletedBy = null);
    void Restore();
}
