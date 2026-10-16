using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.CustomersRelationshipManagement;

public class TicketSubjectRepository(IMainDbContext context) : Repository<TicketSubject>(context), ITicketSubjectRepository
{
    /// <summary>
    /// The tree is shallow in practice, so the ancestors are walked a level at a time rather than
    /// with a recursive query that would tie the repository to one database's dialect.
    /// <para>
    /// Nothing is assigned to <c>Parent</c> here. Loading an ancestor into the change tracker is
    /// enough - the navigation is filled in by relationship fixup - and assigning it would mark a
    /// row as modified that nobody edited.
    /// </para>
    /// </summary>
    public async Task<TicketSubject?> GetWithAncestorsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subject = await _entity.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (subject is null)
            return null;

        // Guarded against a cycle: a subject made its own ancestor by a bad edit would otherwise
        // spin here forever rather than failing a request.
        var seen = new HashSet<Guid> { subject.Id };
        var walked = subject;

        while (walked.ParentId is not null && seen.Add(walked.ParentId.Value))
        {
            var parentId = walked.ParentId.Value;
            var parent = await _entity.FirstOrDefaultAsync(s => s.Id == parentId, cancellationToken);
            if (parent is null)
                break;

            walked = parent;
        }

        return subject;
    }

    public async Task<IReadOnlyList<TicketSubject>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(s => s.IsActive)
            .OrderBy(s => s.Title)
            .ToListAsync(cancellationToken);
    }
}
