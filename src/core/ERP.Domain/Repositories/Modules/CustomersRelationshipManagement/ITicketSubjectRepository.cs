using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

namespace ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;

/// <summary>
/// Ticket subjects are a tree, and a ticket needs the branch of it that applies. A subject carries
/// the promised response and resolution times, which is why it is loaded rather than assumed: the
/// clock a ticket is measured against comes from here.
/// </summary>
public interface ITicketSubjectRepository : IRepository<TicketSubject>
{
    /// <summary>
    /// A subject together with the ancestors it may inherit a service level from, so the effective
    /// one can be worked out without a query per level.
    /// </summary>
    Task<TicketSubject?> GetWithAncestorsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketSubject>> GetActiveAsync(CancellationToken cancellationToken = default);
}
