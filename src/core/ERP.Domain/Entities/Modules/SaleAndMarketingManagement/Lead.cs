namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// Someone who has shown interest but is not yet a customer. Qualifying a lead is what turns it
/// into an opportunity worth pursuing.
/// </summary>
public sealed class Lead : BaseEntity, IBranchScoped
{
    public string FullName { get; private set; } = default!;
    public string? CompanyName { get; private set; }
    public string? JobTitle { get; private set; }

    public LeadSource Source { get; private set; }
    public LeadStatus Status { get; private set; }

    public string MobilePhoneNumberWithRegionCode { get; private set; } = default!;
    public string? Email { get; private set; }
    public Address? Address { get; private set; }

    public Guid? CampaignId { get; private set; }
    public Guid? AssignedToHumanId { get; private set; }
    public Guid? BranchId { get; private set; }

    /// <summary>The customer record this lead became, once it converted.</summary>
    public Guid? ConvertedCustomerId { get; private set; }
    public DateOnly? ConvertedOn { get; private set; }

    /// <summary>How promising the lead looks, out of one hundred.</summary>
    public int? Score { get; private set; }

    public string? DisqualificationReason { get; private set; }

    private readonly List<LeadNote> _notes = [];
    public IReadOnlyList<LeadNote> Notes => _notes;

    private readonly List<LeadInteraction> _interactions = [];
    public IReadOnlyList<LeadInteraction> Interactions => _interactions;

    private readonly List<Opportunity> _opportunities = [];
    public IReadOnlyList<Opportunity> Opportunities => _opportunities;

    private Lead() { }

    public Lead(string fullName, string mobilePhoneNumberWithRegionCode, LeadSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName, nameof(fullName));
        ArgumentException.ThrowIfNullOrWhiteSpace(mobilePhoneNumberWithRegionCode, nameof(mobilePhoneNumberWithRegionCode));
        FullName = fullName.Trim();
        MobilePhoneNumberWithRegionCode = mobilePhoneNumberWithRegionCode.Trim();
        Source = source;
        Status = LeadStatus.New;
    }

    public void Update(string fullName, string? companyName, string? jobTitle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName, nameof(fullName));
        FullName = fullName.Trim();
        CompanyName = string.IsNullOrWhiteSpace(companyName) ? null : companyName.Trim();
        JobTitle = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle.Trim();
    }

    public void SetEmail(string? email) => Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    public void SetAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        Address = address;
    }

    public void AttributeToCampaign(Guid campaignId) => CampaignId = campaignId == Guid.Empty ? null : campaignId;

    public void AssignTo(Guid humanId) => AssignedToHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetScore(int score)
    {
        if (score is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(score), "A score must be between 0 and 100.");
        Score = score;
    }

    public void MarkAsContacted()
    {
        EnsureOpen();
        if (Status == LeadStatus.New)
            Status = LeadStatus.Contacted;
    }

    public void Qualify()
    {
        EnsureOpen();
        Status = LeadStatus.Qualified;
    }

    public void Disqualify(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        EnsureOpen();
        Status = LeadStatus.Disqualified;
        DisqualificationReason = reason.Trim();
    }

    /// <summary>Records that the lead became a customer, which closes it for further work.</summary>
    public void Convert(Guid customerId, DateOnly convertedOn)
    {
        if (Status != LeadStatus.Qualified)
            throw new InvalidOperationException("A lead must be qualified before it can be converted.");
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId is empty.", nameof(customerId));
        ConvertedCustomerId = customerId;
        ConvertedOn = convertedOn;
        Status = LeadStatus.Converted;
        Raise(new LeadConverted(Id, customerId));
    }

    public void AddNote(LeadNote note)
    {
        ArgumentNullException.ThrowIfNull(note, nameof(note));
        _notes.Add(note);
    }

    public void AddInteraction(LeadInteraction interaction)
    {
        ArgumentNullException.ThrowIfNull(interaction, nameof(interaction));
        _interactions.Add(interaction);
        MarkAsContacted();
    }

    public void AddOpportunity(Opportunity opportunity)
    {
        ArgumentNullException.ThrowIfNull(opportunity, nameof(opportunity));
        if (Status is LeadStatus.Disqualified)
            throw new InvalidOperationException("A disqualified lead cannot carry opportunities.");
        _opportunities.Add(opportunity);
    }

    private void EnsureOpen()
    {
        if (Status is LeadStatus.Converted or LeadStatus.Disqualified)
            throw new InvalidOperationException($"The lead is {Status} and is no longer open.");
    }
}
