namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// One step in the life of a cheque, kept so the chain can be audited and each step can be
/// tied to the journal entry it produced.
/// </summary>
public sealed class ChequeOperation : BaseEntity
{
    public Cheque Cheque { get; private set; } = default!;
    public Guid ChequeId { get; private set; }

    public ChequeOperationType Type { get; private set; }
    public DateOnly OccurredOn { get; private set; }

    /// <summary>The state the cheque was left in by this operation.</summary>
    public ChequeStatus ResultingStatus { get; private set; }

    public string? Description { get; private set; }
    public Guid? DocumentId { get; private set; }

    private ChequeOperation() { }

    internal ChequeOperation(Cheque cheque, ChequeOperationType type, DateOnly occurredOn, ChequeStatus resultingStatus, string? description)
    {
        ArgumentNullException.ThrowIfNull(cheque, nameof(cheque));
        Cheque = cheque;
        ChequeId = cheque.Id;
        Type = type;
        OccurredOn = occurredOn;
        ResultingStatus = resultingStatus;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void LinkToDocument(Guid documentId) => DocumentId = documentId == Guid.Empty ? null : documentId;
}
