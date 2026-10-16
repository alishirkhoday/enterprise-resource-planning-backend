namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// A party the organization buys from. What is kept here is what purchasing and accounts payable
/// both need: how to reach them, how they are paid, and how well they perform.
/// </summary>
public sealed class Supplier : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Family { get; private set; }
    public SupplierType Type { get; private set; }
    public SupplierStatus Status { get; private set; }

    public string? MobilePhoneNumber { get; private set; }
    public string Email { get; private set; } = default!;
    public string? StaticPhoneNumber { get; private set; }
    public string? FaxNumber { get; private set; }
    public string? Website { get; private set; }

    public Address? Address { get; private set; }

    /// <summary>Tax identifiers the authority requires on a purchase invoice.</summary>
    public EconomicCode? EconomicCode { get; private set; }
    public string? NationalIdentifier { get; private set; }
    public string? RegistrationNumber { get; private set; }

    /// <summary>The subsidiary account the supplier balance is carried on.</summary>
    public Guid? DetailAccountId { get; private set; }
    public Guid? PaymentTermId { get; private set; }
    public string? PreferredCurrencyCode { get; private set; }

    /// <summary>The most the organization is prepared to owe this supplier at once.</summary>
    public Money? CreditLimit { get; private set; }

    /// <summary>Typical days between placing an order and receiving it.</summary>
    public int? LeadTimeDays { get; private set; }

    /// <summary>Rolling performance score out of one hundred, kept from evaluations.</summary>
    public int? PerformanceScore { get; private set; }

    public string? Description { get; private set; }

    private readonly List<SupplierContact> _contacts = [];
    public IReadOnlyList<SupplierContact> Contacts => _contacts;

    private readonly List<SupplierBankAccount> _bankAccounts = [];
    public IReadOnlyList<SupplierBankAccount> BankAccounts => _bankAccounts;

    private readonly List<SupplierEvaluation> _evaluations = [];
    public IReadOnlyList<SupplierEvaluation> Evaluations => _evaluations;

    private Supplier() { }

    public Supplier(string code, string name, SupplierType type, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));
        Code = code.Trim();
        Name = name.Trim();
        Type = type;
        Email = email.Trim().ToLowerInvariant();
        Status = SupplierStatus.Active;
    }

    public void Update(string name, string? family, SupplierType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        Family = string.IsNullOrWhiteSpace(family) ? null : family.Trim();
        Type = type;
    }

    public void SetContactInformation(string? mobilePhoneNumber, string? staticPhoneNumber, string? faxNumber, string? website)
    {
        MobilePhoneNumber = string.IsNullOrWhiteSpace(mobilePhoneNumber) ? null : mobilePhoneNumber.Trim();
        StaticPhoneNumber = string.IsNullOrWhiteSpace(staticPhoneNumber) ? null : staticPhoneNumber.Trim();
        FaxNumber = string.IsNullOrWhiteSpace(faxNumber) ? null : faxNumber.Trim();
        Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim();
    }

    public void SetAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        Address = address;
    }

    public void SetLegalIdentifiers(string? economicCode, string? nationalIdentifier, string? registrationNumber)
    {
        EconomicCode = string.IsNullOrWhiteSpace(economicCode) ? null : Primitives.ValueObjects.EconomicCode.Create(economicCode);
        NationalIdentifier = string.IsNullOrWhiteSpace(nationalIdentifier) ? null : nationalIdentifier.Trim();
        RegistrationNumber = string.IsNullOrWhiteSpace(registrationNumber) ? null : registrationNumber.Trim();
    }

    public void SetFinancialTerms(Guid? detailAccountId, Guid? paymentTermId, string? preferredCurrencyCode)
    {
        DetailAccountId = detailAccountId == Guid.Empty ? null : detailAccountId;
        PaymentTermId = paymentTermId == Guid.Empty ? null : paymentTermId;
        PreferredCurrencyCode = string.IsNullOrWhiteSpace(preferredCurrencyCode) ? null : preferredCurrencyCode.Trim().ToUpperInvariant();
    }

    public void SetCreditLimit(Money creditLimit)
    {
        ArgumentNullException.ThrowIfNull(creditLimit, nameof(creditLimit));
        if (creditLimit.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(creditLimit), "A credit limit cannot be negative.");
        CreditLimit = creditLimit;
    }

    public void SetLeadTime(int leadTimeDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(leadTimeDays, nameof(leadTimeDays));
        LeadTimeDays = leadTimeDays;
    }

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddContact(SupplierContact contact)
    {
        ArgumentNullException.ThrowIfNull(contact, nameof(contact));
        if (contact.IsPrimary)
            _contacts.ForEach(c => c.UnmarkAsPrimary());
        _contacts.Add(contact);
    }

    public void AddBankAccount(SupplierBankAccount bankAccount)
    {
        ArgumentNullException.ThrowIfNull(bankAccount, nameof(bankAccount));
        if (_bankAccounts.Any(b => b.AccountNumber == bankAccount.AccountNumber))
            throw new InvalidOperationException("This bank account is already registered for the supplier.");
        if (bankAccount.IsDefault)
            _bankAccounts.ForEach(b => b.UnsetDefault());
        _bankAccounts.Add(bankAccount);
    }

    /// <summary>Records an evaluation and refreshes the rolling performance score.</summary>
    public void AddEvaluation(SupplierEvaluation evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation, nameof(evaluation));
        _evaluations.Add(evaluation);
        PerformanceScore = (int)Math.Round(_evaluations.Average(e => e.TotalScore), MidpointRounding.AwayFromZero);
    }

    public void Suspend(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        Status = SupplierStatus.Suspended;
        Description = reason.Trim();
    }

    public void Blacklist(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        Status = SupplierStatus.Blacklisted;
        Description = reason.Trim();
    }

    public void Reinstate() => Status = SupplierStatus.Active;

    /// <summary>Whether a purchase order may be raised against this supplier.</summary>
    public bool CanBeOrderedFrom => Status == SupplierStatus.Active && !IsDeleted;
}
