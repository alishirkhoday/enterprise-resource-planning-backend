namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// What a document on somebody's file is - a contract, an identity paper, a certificate.
/// </summary>
public enum HumanDocumentType
{
    NationalIdentityCard = 0,
    BirthCertificate = 1,
    Passport = 2,
    MilitaryServiceCard = 3,
    EducationalCertificate = 4,
    DrivingLicence = 5,
    WorkPermit = 6,
    MedicalCertificate = 7,
    InsuranceRecord = 8,
    EmploymentContract = 9,
    Other = 99
}
