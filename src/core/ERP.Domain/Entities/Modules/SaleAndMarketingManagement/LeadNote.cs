namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// Something somebody wrote on a lead. Free text, kept in order, because the decision to
/// qualify or drop a lead is made from its history rather than from its fields.
/// </summary>
public sealed class LeadNote : BaseEntity
{
    public Lead Lead { get; private set; } = default!;
    public string Note { get; private set; } = default!;

    private LeadNote()
    {
    }

    public LeadNote(Lead lead, string note)
    {
        ArgumentNullException.ThrowIfNull(lead, nameof(lead));
        ArgumentException.ThrowIfNullOrWhiteSpace(note, nameof(note));
        Lead = lead;
        Note = note;
    }
}
