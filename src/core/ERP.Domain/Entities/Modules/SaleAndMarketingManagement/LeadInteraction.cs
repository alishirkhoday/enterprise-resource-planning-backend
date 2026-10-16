namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// A contact with a lead. Adding one marks the lead as contacted, which is the point: the
/// status follows the work rather than needing to be set separately.
/// </summary>
public sealed class LeadInteraction : BaseEntity
{
    public Lead Lead { get; private set; } = default!;
    public string Subject { get; private set; } = default!;
    public LeadInteractionType Type { get; private set; }
    public string Note { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    private LeadInteraction()
    {
    }

    public LeadInteraction(Lead customer, string subject, LeadInteractionType type, string note, string description)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        ArgumentException.ThrowIfNullOrEmpty(subject, nameof(subject));
        ArgumentException.ThrowIfNullOrEmpty(note, nameof(note));
        ArgumentException.ThrowIfNullOrEmpty(description, nameof(description));
        Lead = customer;
        Subject = subject;
        Type = type;
        Note = note;
        Description = description;
    }
}
