namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Someone the organization is considering hiring. A candidate exists independently of any one
/// vacancy, so the same person can be tracked across several applications over time.
/// </summary>
public sealed class Candidate : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Family { get; private set; } = default!;
    public string FullName => $"{Name} {Family}";

    public HumanGender? Gender { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public NationalId? NationalId { get; private set; }

    public string? MobilePhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public Address? Address { get; private set; }

    public string? CurrentJobTitle { get; private set; }
    public decimal? YearsOfExperience { get; private set; }
    public HumanEducationDegree? HighestDegree { get; private set; }

    /// <summary>Identifier of the stored resume in the attachment store.</summary>
    public Guid? ResumeAttachmentId { get; private set; }

    /// <summary>Where the candidate came from, such as a referral or a job board.</summary>
    public string? Source { get; private set; }

    public bool IsBlacklisted { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<JobApplication> _applications = [];
    public IReadOnlyList<JobApplication> Applications => _applications;

    private Candidate() { }

    public Candidate(string name, string family)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(family, nameof(family));
        Name = name.Trim();
        Family = family.Trim();
    }

    public void SetContactInformation(string? mobilePhoneNumber, string? email)
    {
        MobilePhoneNumber = string.IsNullOrWhiteSpace(mobilePhoneNumber) ? null : mobilePhoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }

    public void SetPersonalInformation(HumanGender gender, DateOnly birthDate)
    {
        if (birthDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentOutOfRangeException(nameof(birthDate), "BirthDate cannot be in the future.");
        Gender = gender;
        BirthDate = birthDate;
    }

    public void SetNationalId(string nationalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nationalId, nameof(nationalId));
        NationalId = NationalId.Create(nationalId);
    }

    public void SetAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        Address = address;
    }

    public void SetProfessionalBackground(string? currentJobTitle, decimal? yearsOfExperience, HumanEducationDegree? highestDegree)
    {
        if (yearsOfExperience.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegative(yearsOfExperience.Value, nameof(yearsOfExperience));
        CurrentJobTitle = string.IsNullOrWhiteSpace(currentJobTitle) ? null : currentJobTitle.Trim();
        YearsOfExperience = yearsOfExperience;
        HighestDegree = highestDegree;
    }

    public void AttachResume(Guid attachmentId) => ResumeAttachmentId = attachmentId == Guid.Empty ? null : attachmentId;

    public void SetSource(string? source) => Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();

    public void SetNotes(string? notes) => Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    public void Blacklist(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        IsBlacklisted = true;
        Notes = reason.Trim();
    }

    public void RemoveFromBlacklist() => IsBlacklisted = false;
}
