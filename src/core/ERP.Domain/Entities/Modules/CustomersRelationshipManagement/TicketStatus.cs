namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// Where a support ticket has got to. Resolved and closed answer different questions: resolved
/// is the desk saying it is done, closed is the customer no longer disputing it.
/// </summary>
public enum TicketStatus
{
    Open = 0,
    InProgress = 1,

    /// <summary>Waiting on the customer to come back with something.</summary>
    AwaitingCustomer = 2,

    Resolved = 3,
    Closed = 4
}
