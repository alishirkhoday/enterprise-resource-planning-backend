namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// How the ticket reached the desk.
/// </summary>
public enum TicketChannel
{
    Portal = 0,
    Email = 1,
    Telephone = 2,
    Chat = 3,
    SocialMedia = 4,
    InPerson = 5
}
