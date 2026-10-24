namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

public sealed partial class Human
{
    public void ChangeHumanContactInformationValue(Guid childId, string value)
    {
        var child = GetChild(childId, _contactInformation);
        child.ChangeValue(value);
    }

    public void SetHumanContactInformationDefault(Guid childId)
    {
        var child = GetChild(childId, _contactInformation);
        child.SetDefault();
    }

    public void UnsetDefaultHumanContactInformation(Guid childId)
    {
        var child = GetChild(childId, _contactInformation);
        child.UnsetDefault();
    }

    public void UpdateHumanAddress(Guid childId, string title, Address address)
    {
        var child = GetChild(childId, _addresses);
        child.Update(title, address);
    }

    public void SetHumanAddressDefault(Guid childId)
    {
        var child = GetChild(childId, _addresses);
        foreach (var sibling in _addresses.Where(sibling => sibling.Id != childId))
            sibling.UnsetDefault();
        child.SetDefault();
    }

    public void UnsetDefaultHumanAddress(Guid childId)
    {
        var child = GetChild(childId, _addresses);
        child.UnsetDefault();
    }

    public void UpdateHumanBankAccount(Guid childId, string bankName, string accountNumber, string cardNumber, string internationalBankAccountNumber)
    {
        var child = GetChild(childId, _bankAccounts);
        child.Update(bankName, accountNumber, cardNumber, internationalBankAccountNumber);
    }

    public void SetHumanBankAccountIsDefault(Guid childId)
    {
        var child = GetChild(childId, _bankAccounts);
        child.SetIsDefault();
    }

    public void UpdateHumanEducation(Guid childId, HumanEducationFieldOfStudy fieldOfStudy, HumanEducationUniversity university, HumanEducationDegree degree, DateOnly startDate, DateOnly? graduationDate = null)
    {
        var child = GetChild(childId, _educations);
        child.Update(fieldOfStudy, university, degree, startDate, graduationDate);
    }

    public void SetHumanEducationGraduationDate(Guid childId, DateOnly graduationDate)
    {
        var child = GetChild(childId, _educations);
        child.SetGraduationDate(graduationDate);
    }

    public void UpdateHumanWorkExperience(Guid childId, Company company, Job job, DateOnly startDate, DateOnly? endDate = null)
    {
        var child = GetChild(childId, _workExperiences);
        child.Update(company, job, startDate, endDate);
    }

    public void SetHumanWorkExperienceDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _workExperiences);
        child.SetDescription(description);
    }

    public void SetHumanDependentNationalId(Guid childId, string nationalId)
    {
        var child = GetChild(childId, _dependents);
        child.SetNationalId(nationalId);
    }

    public void SetHumanDependentCoverage(Guid childId, bool isInsured, bool isTaxDependent)
    {
        var child = GetChild(childId, _dependents);
        child.SetCoverage(isInsured, isTaxDependent);
    }

    public void MarkHumanDependentAsHavingSpecialNeeds(Guid childId)
    {
        var child = GetChild(childId, _dependents);
        child.MarkAsHavingSpecialNeeds();
    }

    public void SetHumanDependentDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _dependents);
        child.SetDescription(description);
    }

    public void SetHumanDocumentIdentification(Guid childId, string? documentNumber, string? issuingAuthority)
    {
        var child = GetChild(childId, _documents);
        child.SetIdentification(documentNumber, issuingAuthority);
    }

    public void SetHumanDocumentValidity(Guid childId, DateOnly? issueDate, DateOnly? expiryDate)
    {
        var child = GetChild(childId, _documents);
        child.SetValidity(issueDate, expiryDate);
    }

    public void AttachFileHumanDocument(Guid childId, Guid attachmentId)
    {
        var child = GetChild(childId, _documents);
        child.AttachFile(attachmentId);
    }

    public void MarkHumanDocumentAsVerified(Guid childId)
    {
        var child = GetChild(childId, _documents);
        child.MarkAsVerified();
    }

    public void SetHumanDocumentDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _documents);
        child.SetDescription(description);
    }

    public void AssessHumanSkill(Guid childId, SkillLevel level, DateOnly assessedOn, string? assessedBy)
    {
        var child = GetChild(childId, _skills);
        child.Assess(level, assessedOn, assessedBy);
    }

    public void SetHumanSkillYearsOfExperience(Guid childId, decimal? years)
    {
        var child = GetChild(childId, _skills);
        child.SetYearsOfExperience(years);
    }

    public void SetHumanSkillDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _skills);
        child.SetDescription(description);
    }
}
