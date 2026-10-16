using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Modules.CorrespondenceManagement;
using ERP.Domain.Repositories.Modules.CorrespondenceManagement;

namespace ERP.Infrastructure.MainDatabase.Repositories.Modules.CorrespondenceManagement;

public class LetterRepository(IMainDbContext context) : Repository<Letter>(context), ILetterRepository
{
    private static readonly LetterStatus[] OpenStatuses =
    [
        LetterStatus.Draft,
        LetterStatus.Registered,
        LetterStatus.Sent,
        LetterStatus.InProgress
    ];

    public async Task<Letter?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return await _entity.FirstOrDefaultAsync(l => l.Number == number, cancellationToken);
    }

    public async Task<Letter?> GetWithRecipientsAndReferralsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(l => l.Recipients)
            .Include(l => l.Referrals)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    /// <summary>
    /// Ordered the way a person would work through it: what is marked immediate, then what has
    /// been waiting longest. Urgency is on the letter, so the sort needs no join.
    /// </summary>
    public async Task<IReadOnlyList<Letter>> GetQueueForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var outstanding = new[] { LetterReferralStatus.Pending, LetterReferralStatus.Seen };

        return await Query
            .Where(l => l.Referrals.Any(r => r.ToUserId == userId && outstanding.Contains(r.Status)))
            .OrderByDescending(l => l.Urgency)
            .ThenBy(l => l.RegisteredOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Letter>> GetByDirectionAsync(LetterDirection direction, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(l => l.Direction == direction && l.LetterDate >= from && l.LetterDate <= to)
            .OrderByDescending(l => l.LetterDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// The exchange around one letter: what it answers, what it follows, and everything that
    /// points back at it. One round trip rather than walking the chain a letter at a time.
    /// </summary>
    public async Task<IReadOnlyList<Letter>> GetThreadAsync(Guid letterId, CancellationToken cancellationToken = default)
    {
        var letter = await Query.FirstOrDefaultAsync(l => l.Id == letterId, cancellationToken);
        if (letter is null)
            return [];

        var antecedents = new[] { letter.InReplyToLetterId, letter.FollowsLetterId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToArray();

        return await Query
            .Where(l => l.Id == letterId
                        || antecedents.Contains(l.Id)
                        || l.InReplyToLetterId == letterId
                        || l.FollowsLetterId == letterId)
            .OrderBy(l => l.LetterDate)
            .ThenBy(l => l.RegisteredOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Letter>> GetOverdueAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(l => l.DueDate != null && l.DueDate < asOf && OpenStatuses.Contains(l.Status))
            .OrderBy(l => l.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LetterReferral>> GetUnseenReferralsAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default)
    {
        return await _context.LetterReferrals
            .AsNoTracking()
            .Include(r => r.Letter)
            .Where(r => r.Status == LetterReferralStatus.Pending && r.ReferredOn < olderThan)
            .OrderBy(r => r.ReferredOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<Letter?> GetByReferralAsync(Guid referralId, CancellationToken cancellationToken = default)
    {
        return await _entity
            .Include(l => l.Referrals)
            .FirstOrDefaultAsync(l => l.Referrals.Any(r => r.Id == referralId), cancellationToken);
    }

    public async Task<string?> GetLastNumberAsync(LetterDirection direction, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(l => l.Direction == direction && (branchId == null || l.BranchId == branchId))
            .OrderByDescending(l => l.Number)
            .Select(l => l.Number)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
