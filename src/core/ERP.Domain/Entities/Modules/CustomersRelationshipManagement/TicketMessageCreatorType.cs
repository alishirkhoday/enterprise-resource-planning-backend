namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// Which side of the conversation wrote a message. The author's identity is separate: this
/// says whether the clock should stop, not who to blame.
/// </summary>
public enum TicketMessageCreatorType
{
    Customer = 0,
    Human = 1
}
