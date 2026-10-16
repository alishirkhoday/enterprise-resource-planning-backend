namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// A category tickets are filed under. Subjects nest, and each carries the service level that
/// tickets under it inherit.
/// </summary>
public sealed class TicketSubject : BaseEntity
{
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }

    public TicketSubject? Parent { get; private set; }
    public Guid? ParentId { get; private set; }

    private readonly List<TicketSubject> _children = [];
    public IReadOnlyList<TicketSubject> Children => _children;

    /// <summary>Minutes within which a first response is promised.</summary>
    public int? ResponseMinutes { get; private set; }

    /// <summary>Minutes within which a resolution is promised.</summary>
    public int? ResolutionMinutes { get; private set; }

    public TicketPriority DefaultPriority { get; private set; }

    /// <summary>The team or person tickets under this subject go to by default.</summary>
    public Guid? DefaultAssigneeHumanId { get; private set; }

    public bool IsActive { get; private set; }

    private TicketSubject() { }

    public TicketSubject(string title, TicketSubject? parent = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        Title = title.Trim();
        Parent = parent;
        ParentId = parent?.Id;
        DefaultPriority = TicketPriority.Normal;
        IsActive = true;
    }

    public void Update(string title, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetParent(TicketSubject? parent)
    {
        if (parent is not null && parent.Id == Id)
            throw new InvalidOperationException("A subject cannot be its own parent.");
        Parent = parent;
        ParentId = parent?.Id;
    }

    public void SetServiceLevel(int responseMinutes, int resolutionMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(responseMinutes, nameof(responseMinutes));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(resolutionMinutes, nameof(resolutionMinutes));
        if (resolutionMinutes < responseMinutes)
            throw new ArgumentOutOfRangeException(nameof(resolutionMinutes), "Resolution cannot be promised sooner than the first response.");
        ResponseMinutes = responseMinutes;
        ResolutionMinutes = resolutionMinutes;
    }

    public void SetDefaults(TicketPriority defaultPriority, Guid? defaultAssigneeHumanId)
    {
        DefaultPriority = defaultPriority;
        DefaultAssigneeHumanId = defaultAssigneeHumanId == Guid.Empty ? null : defaultAssigneeHumanId;
    }

    /// <summary>Walks up the tree for a service level, so only the root needs to define one.</summary>
    public (int Response, int Resolution)? EffectiveServiceLevel()
    {
        var subject = this;
        while (subject is not null)
        {
            if (subject.ResponseMinutes.HasValue && subject.ResolutionMinutes.HasValue)
                return (subject.ResponseMinutes.Value, subject.ResolutionMinutes.Value);
            subject = subject.Parent;
        }
        return null;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
