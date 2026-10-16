namespace ERP.Domain.Entities.Organizations;

/// <summary>
/// The legal entity the installation keeps books for. Everything financial ultimately rolls up to one,
/// and a group of companies is modelled as several organizations sharing one database.
/// </summary>
public sealed class Organization : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? TradeName { get; private set; }
    public OrganizationType Type { get; private set; }

    /// <summary>National identifier of the legal entity, in Iran the eleven digit شناسه ملی.</summary>
    public string? RegistrationNumber { get; private set; }
    public string? NationalIdentifier { get; private set; }
    public EconomicCode? EconomicCode { get; private set; }

    public Address? Address { get; private set; }
    public string? Telephone { get; private set; }
    public string? FaxNumber { get; private set; }
    public string? Email { get; private set; }
    public string? Website { get; private set; }

    /// <summary>The currency the books are kept in.</summary>
    public Guid BaseCurrencyId { get; private set; }

    public Guid? DefaultWorkCalendarId { get; private set; }
    public DateOnly? EstablishmentDate { get; private set; }
    public bool IsActive { get; private set; }

    private Organization() { }

    public Organization(string name, OrganizationType type, Guid baseCurrencyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        if (baseCurrencyId == Guid.Empty)
            throw new ArgumentException("BaseCurrencyId is empty.", nameof(baseCurrencyId));
        Name = name.Trim();
        Type = type;
        BaseCurrencyId = baseCurrencyId;
        IsActive = true;
    }

    public void Update(string name, string? tradeName, OrganizationType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        TradeName = string.IsNullOrWhiteSpace(tradeName) ? null : tradeName.Trim();
        Type = type;
    }

    public void SetLegalIdentifiers(string? registrationNumber, string? nationalIdentifier, string? economicCode)
    {
        RegistrationNumber = string.IsNullOrWhiteSpace(registrationNumber) ? null : registrationNumber.Trim();
        NationalIdentifier = string.IsNullOrWhiteSpace(nationalIdentifier) ? null : nationalIdentifier.Trim();
        EconomicCode = string.IsNullOrWhiteSpace(economicCode) ? null : Primitives.ValueObjects.EconomicCode.Create(economicCode);
    }

    public void SetAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        Address = address;
    }

    public void SetContactInformation(string? telephone, string? faxNumber, string? email, string? website)
    {
        Telephone = string.IsNullOrWhiteSpace(telephone) ? null : telephone.Trim();
        FaxNumber = string.IsNullOrWhiteSpace(faxNumber) ? null : faxNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim();
    }

    public void SetBaseCurrency(Guid currencyId)
    {
        if (currencyId == Guid.Empty)
            throw new ArgumentException("CurrencyId is empty.", nameof(currencyId));
        BaseCurrencyId = currencyId;
    }

    public void SetDefaultWorkCalendar(Guid workCalendarId) => DefaultWorkCalendarId = workCalendarId == Guid.Empty ? null : workCalendarId;

    public void SetEstablishmentDate(DateOnly date)
    {
        if (date > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentOutOfRangeException(nameof(date), "EstablishmentDate cannot be in the future.");
        EstablishmentDate = date;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
