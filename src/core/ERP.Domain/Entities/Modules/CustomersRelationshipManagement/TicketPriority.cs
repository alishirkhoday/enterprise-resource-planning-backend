namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// How urgent a ticket is. Defaults from the subject rather than from whoever raised it, so it
/// reflects the agreement rather than the mood.
/// </summary>
public enum TicketPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3,

    /// <summary>The customer cannot operate at all until it is fixed.</summary>
    Critical = 4
}
