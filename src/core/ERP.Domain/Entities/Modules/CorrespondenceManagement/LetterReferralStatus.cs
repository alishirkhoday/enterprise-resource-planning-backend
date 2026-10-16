namespace ERP.Domain.Entities.Modules.CorrespondenceManagement;

/// <summary>
/// Where one referral has got to - ارجاع. A letter can be referred to several people, and
/// each of them answers separately.
/// </summary>
public enum LetterReferralStatus
{
    /// <summary>In their queue, not yet opened.</summary>
    Pending = 0,

    /// <summary>Opened, not yet answered.</summary>
    Seen = 1,

    Answered = 2,

    /// <summary>Taken back before it was answered.</summary>
    Withdrawn = 3
}
