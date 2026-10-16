namespace ERP.Domain.Entities.Modules.CorrespondenceManagement;

/// <summary>
/// Where a letter has got to in the registry - دبیرخانه.
/// </summary>
public enum LetterStatus
{
    /// <summary>An outgoing letter being written.</summary>
    Draft = 0,

    /// <summary>Entered in the register and given a number.</summary>
    Registered = 1,

    /// <summary>Dispatched.</summary>
    Sent = 2,

    /// <summary>Sitting in somebody's queue.</summary>
    InProgress = 3,

    /// <summary>Dealt with, with every referral answered.</summary>
    Closed = 4,

    /// <summary>Filed away. The number stays valid so later citations still resolve.</summary>
    Archived = 5
}
