using ERP.Domain.Entities.Modules.CorrespondenceManagement;

namespace ERP.Domain.Repositories.Modules.CorrespondenceManagement;
/// <summary>
/// The registry - دبیرخانه. Incoming and outgoing letters, their numbers and who they were
/// referred to.
/// </summary>
public interface ILetterRepository : IRepository<Letter>
{
    /// <summary>The registration number is how a letter is cited, so this is the primary lookup.</summary>
    Task<Letter?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    Task<Letter?> GetWithRecipientsAndReferralsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Somebody's correspondence queue: letters referred to them that they have not answered,
    /// most urgent first.
    /// </summary>
    Task<IReadOnlyList<Letter>> GetQueueForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Letter>> GetByDirectionAsync(LetterDirection direction, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// The thread a letter belongs to - what it answers, and everything that answers it - so the
    /// exchange can be read in order rather than one letter at a time.
    /// </summary>
    Task<IReadOnlyList<Letter>> GetThreadAsync(Guid letterId, CancellationToken cancellationToken = default);

    /// <summary>Open letters past their due date, so nothing quietly goes unanswered.</summary>
    Task<IReadOnlyList<Letter>> GetOverdueAsync(DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>Referrals nobody has opened yet, which is where a register silently stalls.</summary>
    Task<IReadOnlyList<LetterReferral>> GetUnseenReferralsAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// The letter a referral belongs to, loaded with its referrals. Answering one is a change to
    /// the letter as a whole - it may be the answer that lets it be filed - so the root is what
    /// comes back rather than the referral on its own.
    /// </summary>
    Task<Letter?> GetByReferralAsync(Guid referralId, CancellationToken cancellationToken = default);

    /// <summary>The highest number issued in a register, for assigning the next one.</summary>
    Task<string?> GetLastNumberAsync(LetterDirection direction, Guid? branchId = null, CancellationToken cancellationToken = default);
}
