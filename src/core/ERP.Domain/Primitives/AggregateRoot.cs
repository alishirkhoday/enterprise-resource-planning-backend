namespace ERP.Domain.Primitives;

/// <summary>The consistency boundary that can be loaded and saved through a repository.</summary>
public abstract class AggregateRoot : BaseEntity, IAggregateRoot<Guid>
{
    public new void Delete(string? deletedBy = null) => base.Delete(deletedBy);

    public new void Restore() => base.Restore();
}
