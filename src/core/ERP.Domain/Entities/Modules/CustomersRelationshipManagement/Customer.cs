using ERP.Domain.Entities.Modules.OrderManagement;

namespace ERP.Domain.Entities.Modules.CustomersRelationshipManagement;

/// <summary>
/// A party the organization sells to. Everything sales and receivables need about them lives here:
/// how to reach them, what they may owe, and on what terms they buy.
/// </summary>
public sealed class Customer : BaseEntity, IBranchScoped
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Family { get; private set; }
    public CustomerType Type { get; private set; }
    public CustomerStatus Status { get; private set; }

    public string DisplayName => Type == CustomerType.Individual ? $"{Name} {Family}".Trim() : Name;

    public NationalId? NationalId { get; private set; }
    public PassportId? PassportId { get; private set; }
    public EconomicCode? EconomicCode { get; private set; }
    public string? RegistrationNumber { get; private set; }

    public string? MobilePhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public string? StaticPhoneNumber { get; private set; }
    public string? FaxNumber { get; private set; }
    public string? Website { get; private set; }

    public Guid? CustomerGroupId { get; private set; }
    public Guid? PriceListId { get; private set; }
    public Guid? PaymentTermId { get; private set; }

    /// <summary>The subsidiary account the customer balance is carried on.</summary>
    public Guid? DetailAccountId { get; private set; }

    /// <summary>The most the customer is allowed to owe at once.</summary>
    public Money? CreditLimit { get; private set; }

    /// <summary>The employee answerable for the relationship.</summary>
    public Guid? SalesRepHumanId { get; private set; }

    public Guid? BranchId { get; private set; }
    public DateOnly? FirstPurchaseDate { get; private set; }
    public string? Description { get; private set; }

    private readonly List<CustomerAddress> _addresses = [];
    public IReadOnlyList<CustomerAddress> Addresses => _addresses;

    private readonly List<CustomerContact> _contacts = [];
    public IReadOnlyList<CustomerContact> Contacts => _contacts;

    private readonly List<Order> _orders = [];
    public IReadOnlyList<Order> Orders => _orders;

    private readonly List<Invoice> _invoices = [];
    public IReadOnlyList<Invoice> Invoices => _invoices;

    private readonly List<Ticket> _tickets = [];
    public IReadOnlyList<Ticket> Tickets => _tickets;

    private Customer() { }

    public Customer(string code, string name, CustomerType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Code = code.Trim();
        Name = name.Trim();
        Type = type;
        Status = CustomerStatus.Active;
    }

    public void Update(string name, string? family, CustomerType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        Family = string.IsNullOrWhiteSpace(family) ? null : family.Trim();
        Type = type;
    }

    public void SetNationalId(string nationalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nationalId, nameof(nationalId));
        NationalId = NationalId.Create(nationalId);
    }

    public void SetPassportId(string passportId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passportId, nameof(passportId));
        PassportId = PassportId.Create(passportId);
    }

    public void SetLegalIdentifiers(string? economicCode, string? registrationNumber)
    {
        if (Type != CustomerType.Organization)
            throw new InvalidOperationException("Legal identifiers only apply to an organization customer.");
        EconomicCode = string.IsNullOrWhiteSpace(economicCode) ? null : Primitives.ValueObjects.EconomicCode.Create(economicCode);
        RegistrationNumber = string.IsNullOrWhiteSpace(registrationNumber) ? null : registrationNumber.Trim();
    }

    public void SetContactInformation(string? mobilePhoneNumber, string? email, string? staticPhoneNumber, string? faxNumber, string? website)
    {
        MobilePhoneNumber = string.IsNullOrWhiteSpace(mobilePhoneNumber) ? null : mobilePhoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        StaticPhoneNumber = string.IsNullOrWhiteSpace(staticPhoneNumber) ? null : staticPhoneNumber.Trim();
        FaxNumber = string.IsNullOrWhiteSpace(faxNumber) ? null : faxNumber.Trim();
        Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim();
    }

    public void SetCommercialTerms(Guid? customerGroupId, Guid? priceListId, Guid? paymentTermId)
    {
        CustomerGroupId = customerGroupId == Guid.Empty ? null : customerGroupId;
        PriceListId = priceListId == Guid.Empty ? null : priceListId;
        PaymentTermId = paymentTermId == Guid.Empty ? null : paymentTermId;
    }

    public void LinkToDetailAccount(Guid detailAccountId) =>
        DetailAccountId = detailAccountId == Guid.Empty ? null : detailAccountId;

    public void SetCreditLimit(Money creditLimit)
    {
        ArgumentNullException.ThrowIfNull(creditLimit, nameof(creditLimit));
        if (creditLimit.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(creditLimit), "A credit limit cannot be negative.");
        CreditLimit = creditLimit;
    }

    public void AssignSalesRep(Guid humanId) => SalesRepHumanId = humanId == Guid.Empty ? null : humanId;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void RecordFirstPurchase(DateOnly date) => FirstPurchaseDate ??= date;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void AddAddress(CustomerAddress address)
    {
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        if (_addresses.Count == 0)
            address.SetDefault();
        else if (address.IsDefault)
            _addresses.ForEach(a => a.UnsetDefault());
        _addresses.Add(address);
    }

    public void AddContact(CustomerContact contact)
    {
        ArgumentNullException.ThrowIfNull(contact, nameof(contact));
        if (contact.IsPrimary)
            _contacts.ForEach(c => c.UnmarkAsPrimary());
        _contacts.Add(contact);
    }

    public CustomerAddress? DefaultAddress() => _addresses.FirstOrDefault(a => a.IsDefault);

    public CustomerAddress? BillingAddress() =>
        _addresses.FirstOrDefault(a => a.Type is CustomerAddressType.Billing or CustomerAddressType.Both);

    /// <summary>Whether taking on a further debt of this size would stay inside the credit limit.</summary>
    public bool CanTakeOnDebt(Money currentBalance, Money additionalAmount)
    {
        ArgumentNullException.ThrowIfNull(currentBalance, nameof(currentBalance));
        ArgumentNullException.ThrowIfNull(additionalAmount, nameof(additionalAmount));
        if (CreditLimit is null)
            return true;
        return currentBalance.Add(additionalAmount).Amount <= CreditLimit.Amount;
    }

    public void Suspend(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        Status = CustomerStatus.Suspended;
        Description = reason.Trim();
    }

    public void Blacklist(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));
        Status = CustomerStatus.Blacklisted;
        Description = reason.Trim();
    }

    public void Reinstate() => Status = CustomerStatus.Active;

    public bool CanBeSoldTo => Status == CustomerStatus.Active && !IsDeleted;
}
