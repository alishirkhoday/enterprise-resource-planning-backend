namespace ERP.Domain.Entities.Modules.CorrespondenceManagement;

/// <summary>
/// One addressee of a letter. A letter usually goes to one place and is copied to several others,
/// and who was on copy is part of the record.
/// </summary>
public sealed class LetterRecipient : BaseEntity
{
    public Letter Letter { get; private set; } = default!;
    public Guid LetterId { get; private set; }

    public LetterRecipientKind Kind { get; private set; }

    /// <summary>Written out, because most addressees are outside this system.</summary>
    public string Name { get; private set; } = default!;

    public string? Organization { get; private set; }
    public string? PostalAddress { get; private set; }
    public string? Email { get; private set; }

    /// <summary>Filled in when the addressee is someone this system already knows.</summary>
    public Guid? UserId { get; private set; }
    public Guid? HumanId { get; private set; }

    public string? Description { get; private set; }

    private LetterRecipient() { }

    public LetterRecipient(Letter letter, LetterRecipientKind kind, string name)
    {
        ArgumentNullException.ThrowIfNull(letter, nameof(letter));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Letter = letter;
        LetterId = letter.Id;
        Kind = kind;
        Name = name.Trim();
    }

    internal void SetContactDetails(string? organization, string? postalAddress, string? email)
    {
        Organization = string.IsNullOrWhiteSpace(organization) ? null : organization.Trim();
        PostalAddress = string.IsNullOrWhiteSpace(postalAddress) ? null : postalAddress.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : Normalization.NormalizeEmail(email);
    }

    internal void IdentifyInternally(Guid? userId, Guid? humanId)
    {
        UserId = userId == Guid.Empty ? null : userId;
        HumanId = humanId == Guid.Empty ? null : humanId;
    }

    internal void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public bool IsInternal => UserId.HasValue || HumanId.HasValue;
}
