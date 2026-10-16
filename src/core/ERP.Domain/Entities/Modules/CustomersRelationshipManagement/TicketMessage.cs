namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// One message in a ticket conversation. The first message from the desk is what stops the
/// response clock, which the ticket works out from the author rather than being told.
/// </summary>
public sealed class TicketMessage : BaseEntity
{
    public Ticket Ticket { get; private set; } = default!;
    public Guid TicketId { get; private set; }
    /// <summary>
    /// Who wrote it. Held as text rather than as a foreign key because the two kinds of author -
    /// a customer and an employee - live in different tables and neither owns the other.
    /// </summary>
    public string CreatorId { get; private set; } = default!;

    public TicketMessageCreatorType CreatorType { get; private set; }

    public string Message { get; private set; } = default!;
    public string? ReplayMessageId { get; private set; }

    private TicketMessage() { }

    public TicketMessage(Ticket ticket, TicketMessageCreatorType creatorType, string creatorId, string message, string? replayMessageId = null)
    {
        ArgumentNullException.ThrowIfNull(ticket, nameof(ticket));
        ArgumentException.ThrowIfNullOrWhiteSpace(creatorId, nameof(creatorId));
        ArgumentException.ThrowIfNullOrEmpty(message, nameof(message));
        Ticket = ticket;
        TicketId = ticket.Id;
        CreatorType = creatorType;
        CreatorId = creatorId.Trim();
        Message = message.Trim();
        ReplayMessageId = replayMessageId;
    }
}
