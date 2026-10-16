namespace ERP.Domain.Entities.Files;

/// <summary>
/// What an attached file is, which decides who may see it and how long it is kept.
/// </summary>
public enum AttachmentCategory
{
    Contract = 0,
    Invoice = 1,
    Receipt = 2,
    IdentityDocument = 3,
    Certificate = 4,
    Photo = 5,
    TechnicalDrawing = 6,
    Report = 7,
    Correspondence = 8,
    Other = 99
}
