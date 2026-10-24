namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// A job as a definition - شرح شغل: what the work is, regardless of who is doing it or
/// whether anybody is.
/// </summary>
public sealed class Job : AggregateRoot
{
    public string Name { get; private set; } = default!;

    private Job() { }

    public Job(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
    }

    public void Update(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
    }
}
