namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// A personal document held on file, such as an identity card or a work permit. Expiry is tracked
/// so the organization can be warned before a permit lapses.
/// </summary>
public sealed class HumanDocument : BaseEntity
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }

    public HumanDocumentType Type { get; private set; }
    public string Title { get; private set; } = default!;
    public string? DocumentNumber { get; private set; }

    public DateOnly? IssueDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public string? IssuingAuthority { get; private set; }

    /// <summary>Identifier of the stored file in the attachment store.</summary>
    public Guid? AttachmentId { get; private set; }

    public bool IsVerified { get; private set; }
    public string? Description { get; private set; }

    private HumanDocument() { }

    public HumanDocument(Human human, HumanDocumentType type, string title)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        Human = human;
        HumanId = human.Id;
        Type = type;
        Title = title.Trim();
    }

    internal void SetIdentification(string? documentNumber, string? issuingAuthority)
    {
        DocumentNumber = string.IsNullOrWhiteSpace(documentNumber) ? null : documentNumber.Trim();
        IssuingAuthority = string.IsNullOrWhiteSpace(issuingAuthority) ? null : issuingAuthority.Trim();
    }

    internal void SetValidity(DateOnly? issueDate, DateOnly? expiryDate)
    {
        if (issueDate.HasValue && expiryDate.HasValue && expiryDate.Value < issueDate.Value)
            throw new ArgumentOutOfRangeException(nameof(expiryDate), "ExpiryDate cannot be earlier than IssueDate.");
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
    }

    internal void AttachFile(Guid attachmentId) => AttachmentId = attachmentId == Guid.Empty ? null : attachmentId;

    internal void MarkAsVerified() => IsVerified = true;

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public bool IsExpiredOn(DateOnly date) => ExpiryDate.HasValue && ExpiryDate.Value < date;

    public int? DaysUntilExpiry(DateOnly from) => ExpiryDate.HasValue ? ExpiryDate.Value.DayNumber - from.DayNumber : null;
}
