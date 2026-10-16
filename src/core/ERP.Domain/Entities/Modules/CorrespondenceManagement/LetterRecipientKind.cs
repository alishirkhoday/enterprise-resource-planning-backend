namespace ERP.Domain.Entities.Modules.CorrespondenceManagement;

/// <summary>
/// Whether a recipient is the addressee or only copied - گیرنده یا رونوشت.
/// </summary>
public enum LetterRecipientKind
{
    /// <summary>The addressee the letter is actually for.</summary>
    Primary = 0,

    /// <summary>Copied in.</summary>
    Copy = 1,

    /// <summary>Copied in without the other addressees being told.</summary>
    BlindCopy = 2
}
