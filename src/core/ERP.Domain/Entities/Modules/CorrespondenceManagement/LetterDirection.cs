namespace ERP.Domain.Entities.Modules.CorrespondenceManagement;

/// <summary>
/// Whether a letter came in or went out - وارده یا صادره. Each direction has its own
/// numbering in the registry.
/// </summary>
public enum LetterDirection
{
    /// <summary>Received from outside.</summary>
    Inbound = 0,

    /// <summary>Sent out.</summary>
    Outbound = 1,

    /// <summary>Between parts of this organization, which still needs a number to be citable.</summary>
    Internal = 2
}
