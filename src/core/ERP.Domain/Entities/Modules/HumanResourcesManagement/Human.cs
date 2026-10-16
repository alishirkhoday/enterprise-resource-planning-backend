using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Entities.Users;

namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// A person the company employs - پرسنل.
/// 
/// Separate from the User account they may sign in with: plenty of employees never log in, and
/// some accounts belong to nobody. What they are paid and where they sit lives on their
/// contracts rather than here, because both change while the person does not.
/// </summary>
public sealed class Human : BaseEntity, IBranchScoped
{
    public User User { get; private set; } = default!;
    public Guid UserId { get; private set; }

    /// <summary>The personnel code the organization knows this person by.</summary>
    public string? EmployeeNumber { get; private set; }

    public string Name { get; private set; } = default!;
    public string Family { get; private set; } = default!;
    public string FullName => $"{Name} {Family}";

    public string? FatherName { get; private set; }
    public HumanGender Gender { get; private set; }
    public DateOnly BirthDate { get; private set; }
    public string? PlaceOfBirth { get; private set; }
    public bool MaritalStatus { get; private set; }

    public NationalId? NationalId { get; private set; }
    public PassportId? PassportId { get; private set; }
    public string? BirthCertificateNumber { get; private set; }
    public MilitaryServiceStatus? MilitaryService { get; private set; }

    public HumanStatus Status { get; private set; }
    public DateOnly? HireDate { get; private set; }
    public DateOnly? TerminationDate { get; private set; }

    /// <summary>The person this employee reports to, which is what forms the organization chart.</summary>
    public Human? Manager { get; private set; }
    public Guid? ManagerId { get; private set; }

    public Guid? BranchId { get; private set; }
    public Guid? WorkCalendarId { get; private set; }

    private readonly List<Human> _directReports = [];
    public IReadOnlyList<Human> DirectReports => _directReports;

    private readonly List<HumanContactInformation> _contactInformation = [];
    public IReadOnlyList<HumanContactInformation> ContactInformation => _contactInformation;

    private readonly List<HumanAddress> _addresses = [];
    public IReadOnlyList<HumanAddress> Addresses => _addresses;

    private readonly List<HumanBankAccount> _bankAccounts = [];
    public IReadOnlyList<HumanBankAccount> BankAccounts => _bankAccounts;

    private readonly List<HumanEducation> _educations = [];
    public IReadOnlyList<HumanEducation> Educations => _educations;

    private readonly List<HumanWorkExperience> _workExperiences = [];
    public IReadOnlyList<HumanWorkExperience> WorkExperiences => _workExperiences;

    private readonly List<HumanContract> _contracts = [];
    public IReadOnlyList<HumanContract> Contracts => _contracts;

    private readonly List<HumanAttendanceRecord> _attendanceRecords = [];
    public IReadOnlyList<HumanAttendanceRecord> AttendanceRecords => _attendanceRecords;

    private readonly List<HumanRequest> _leaveRequests = [];
    public IReadOnlyList<HumanRequest> LeaveRequests => _leaveRequests;

    private readonly List<Payroll> _payrolls = [];
    public IReadOnlyList<Payroll> Payrolls => _payrolls;

    private readonly List<HumanDependent> _dependents = [];
    public IReadOnlyList<HumanDependent> Dependents => _dependents;

    private readonly List<HumanDocument> _documents = [];
    public IReadOnlyList<HumanDocument> Documents => _documents;

    private readonly List<HumanSkill> _skills = [];
    public IReadOnlyList<HumanSkill> Skills => _skills;

    private Human() { }

    public Human(User user, string name, string family, HumanGender gender, DateOnly birthDate, bool maritalStatus)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        Validate(name, family, birthDate);
        User = user;
        UserId = user.Id;
        Name = name.Trim();
        Family = family.Trim();
        BirthDate = birthDate;
        Gender = gender;
        MaritalStatus = maritalStatus;
        Status = HumanStatus.Active;
    }

    public void Update(string name, string family, HumanGender gender, DateOnly birthDate, bool maritalStatus)
    {
        Validate(name, family, birthDate);
        Name = name.Trim();
        Family = family.Trim();
        Gender = gender;
        BirthDate = birthDate;
        MaritalStatus = maritalStatus;
    }

    public void SetEmployeeNumber(string employeeNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeNumber, nameof(employeeNumber));
        EmployeeNumber = employeeNumber.Trim();
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

    public void SetCivilInformation(string? fatherName, string? birthCertificateNumber, string? placeOfBirth)
    {
        FatherName = string.IsNullOrWhiteSpace(fatherName) ? null : fatherName.Trim();
        BirthCertificateNumber = string.IsNullOrWhiteSpace(birthCertificateNumber) ? null : birthCertificateNumber.Trim();
        PlaceOfBirth = string.IsNullOrWhiteSpace(placeOfBirth) ? null : placeOfBirth.Trim();
    }

    public void SetMilitaryServiceStatus(MilitaryServiceStatus status)
    {
        if (Gender != HumanGender.Male)
            throw new InvalidOperationException("Military service status only applies to male employees.");
        MilitaryService = status;
    }

    public void ChangeStatus(HumanStatus status) => Status = status;

    public void Hire(DateOnly hireDate)
    {
        if (hireDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentOutOfRangeException(nameof(hireDate), "HireDate cannot be in the future.");
        HireDate = hireDate;
        TerminationDate = null;
        Status = HumanStatus.Active;
    }

    public void Terminate(DateOnly terminationDate)
    {
        if (HireDate.HasValue && terminationDate < HireDate.Value)
            throw new ArgumentOutOfRangeException(nameof(terminationDate), "TerminationDate cannot be earlier than HireDate.");
        if (Status == HumanStatus.Terminated)
            return;
        TerminationDate = terminationDate;
        Status = HumanStatus.Terminated;
        Raise(new EmployeeTerminated(Id, terminationDate));
    }

    public void AssignManager(Human? manager)
    {
        if (manager is not null && manager.Id == Id)
            throw new InvalidOperationException("An employee cannot report to themselves.");
        Manager = manager;
        ManagerId = manager?.Id;
    }

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void AssignWorkCalendar(Guid workCalendarId) => WorkCalendarId = workCalendarId == Guid.Empty ? null : workCalendarId;

    public void AddContactInformation(HumanContactInformation contactInformation)
    {
        ArgumentNullException.ThrowIfNull(contactInformation, nameof(contactInformation));

        // prevent duplicates (same type + value)
        if (_contactInformation.Any(c => c.Type == contactInformation.Type && string.Equals(c.Value, contactInformation.Value)))
        {
            throw new InvalidOperationException("Contact information already exists.");
        }

        var existingDefault = _contactInformation.FirstOrDefault(c => c.Type == contactInformation.Type && c.IsDefault);

        // if this is the first contact of this type, force it to be default.
        if (existingDefault is null && !contactInformation.IsDefault)
        {
            contactInformation.SetDefault();
        }

        // if incoming is default, unset existing default.
        if (contactInformation.IsDefault && existingDefault is not null)
        {
            existingDefault.UnsetDefault();
        }

        _contactInformation.Add(contactInformation);
    }

    public void AddAddress(HumanAddress address)
    {
        ArgumentNullException.ThrowIfNull(address, nameof(address));
        if (_addresses.Count == 0)
            address.SetDefault();
        else if (address.IsDefault)
            _addresses.ForEach(a => a.UnsetDefault());
        _addresses.Add(address);
    }

    public void AddBankAccount(HumanBankAccount bankAccount)
    {
        ArgumentNullException.ThrowIfNull(bankAccount, nameof(bankAccount));
        if (_bankAccounts.Any(b => b.AccountNumber == bankAccount.AccountNumber))
            throw new InvalidOperationException("This bank account is already registered for the employee.");
        _bankAccounts.Add(bankAccount);
    }

    public void AddDependent(HumanDependent dependent)
    {
        ArgumentNullException.ThrowIfNull(dependent, nameof(dependent));
        _dependents.Add(dependent);
    }

    public void AddDocument(HumanDocument document)
    {
        ArgumentNullException.ThrowIfNull(document, nameof(document));
        _documents.Add(document);
    }

    public void AddSkill(HumanSkill skill)
    {
        ArgumentNullException.ThrowIfNull(skill, nameof(skill));
        if (_skills.Any(s => s.SkillId == skill.SkillId))
            throw new InvalidOperationException("This skill is already registered for the employee.");
        _skills.Add(skill);
    }

    /// <summary>The contract that is in force on the given date, if any.</summary>
    public HumanContract? ContractOn(DateOnly date) =>
        _contracts.FirstOrDefault(c => c.Period.Contains(date) && c.TerminationDate is null);

    public int AgeOn(DateOnly date)
    {
        var age = date.Year - BirthDate.Year;
        if (BirthDate > date.AddYears(-age))
            age--;
        return age;
    }

    private static void Validate(string name, string family, DateOnly birthDate)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentException.ThrowIfNullOrEmpty(family, nameof(family));

        var min = new DateOnly(1700, 1, 1);
        var max = DateOnly.FromDateTime(DateTime.UtcNow);

        if (birthDate < min)
            throw new ArgumentOutOfRangeException(nameof(birthDate), $"BirthDate cannot be earlier than {min:yyyy/MM/dd}.");

        if (birthDate > max)
            throw new ArgumentOutOfRangeException(nameof(birthDate), "BirthDate cannot be in the future.");
    }
}
