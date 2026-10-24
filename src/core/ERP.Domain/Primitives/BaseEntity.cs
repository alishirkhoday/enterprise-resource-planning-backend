namespace ERP.Domain.Primitives;

/// <summary>Identity, audit information and event recording shared by roots and their children.</summary>
public abstract class BaseEntity : IAuditable, ISoftDeletable
{
    public Guid Id { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Facts this entity has recorded but nobody has acted on yet. Infrastructure is expected to
    /// dispatch and then clear them as part of saving, and never to persist the list itself.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// The creation time in UTC. Persist this value if you need auditability.
    /// </summary>
    public DateTimeOffset CreationDateTime { get; protected set; }

    public string? CreatedBy { get; protected set; }

    /// <summary>
    /// Optional concurrency token (EF Core can map this to a rowversion or a string stamp).
    /// </summary>
    public string? ConcurrencyStamp { get; protected set; }

    public string? ModifiedBy { get; protected set; }
    public DateTimeOffset? ModificationDateTime { get; protected set; }

    public bool IsDeleted { get; protected set; }
    public string? DeletedBy { get; protected set; }
    public DateTimeOffset? DeletionDateTime { get; protected set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreationDateTime = DateTimeOffset.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Records a fact that has already happened. The event is held until infrastructure dispatches
    /// it, so a handler can never observe a change that the transaction went on to roll back.
    /// </summary>
    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent, nameof(domainEvent));
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Records who created the entity. Only takes effect once.
    /// </summary>
    public void SetCreatedBy(string? createdBy)
    {
        if (CreatedBy is not null)
            return;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Updates audit fields and rotates concurrency stamp.
    /// </summary>
    public void Touch(string? modifiedBy = null)
    {
        ModifiedBy = modifiedBy;
        ModificationDateTime = DateTimeOffset.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Marks the entity as deleted without removing the row, so history and references stay intact.
    /// </summary>
    internal void Delete(string? deletedBy = null)
    {
        if (IsDeleted)
            return;
        IsDeleted = true;
        DeletedBy = deletedBy;
        DeletionDateTime = DateTimeOffset.UtcNow;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    internal void Restore()
    {
        if (!IsDeleted)
            return;
        IsDeleted = false;
        DeletedBy = null;
        DeletionDateTime = null;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    protected void EnsureCanAddChild<T>(T child, Guid ownerId, IReadOnlyCollection<T> children)
        where T : BaseEntity
    {
        ArgumentNullException.ThrowIfNull(child);
        if (ownerId != Id)
            throw new InvalidOperationException("The child belongs to a different owner.");
        if (children.Any(existing => existing.Id == child.Id))
            throw new InvalidOperationException("The child is already attached to this owner.");
    }

    protected T GetChild<T>(Guid childId, IEnumerable<T> children) where T : BaseEntity =>
        children.SingleOrDefault(child => child.Id == childId)
        ?? throw new InvalidOperationException("The child is not attached to this owner.");

    public static bool operator ==(BaseEntity? firstEntity, BaseEntity? secondEntity)
    {
        if (ReferenceEquals(firstEntity, secondEntity)) return true;
        return firstEntity is not null && secondEntity is not null && firstEntity.Equals(secondEntity);
    }

    public static bool operator !=(BaseEntity? firstEntity, BaseEntity? secondEntity) => !(firstEntity == secondEntity);

    /// <summary>
    /// Two entities are the same when they are the same kind of thing with the same identity. The
    /// type check keeps a customer and a supplier that happen to share an identifier apart.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return obj is BaseEntity entity && entity.Id == Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
