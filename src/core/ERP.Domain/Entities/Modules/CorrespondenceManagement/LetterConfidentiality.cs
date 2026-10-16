namespace ERP.Domain.Entities.Modules.CorrespondenceManagement;

/// <summary>
/// How restricted a letter is - عادی، محرمانه، سری. Decides who may see it at all.
/// </summary>
public enum LetterConfidentiality
{
    Normal = 0,
    Confidential = 1,
    Secret = 2
}
