namespace ERP.Domain.Entities.Security;

/// <summary>
/// The roles the system itself relies on, named rather than looked up by string at call sites.
/// </summary>
public enum RoleName
{
    Shareholder,

    Worker,

    Driver,

    SellerSpecialist,
    SellerManager,

    SupportSpecialist,
    SupportManager,

    Employee,

    Graphist,

    Programmer,

    ProductOwner,
    ProductManager,

    InformationCommunicationTechnology,
    InformationCommunicationTechnologySpecialist,
    InformationCommunicationTechnologyManager,

    Accountant,
    AccountantSpecialist,
    FinancialManager,

    HumanResources,
    HumanResourcesManager,

    LogisticsSpecialist,
    LogisticsManager,

    WarehouseWorker,
    WarehouseManager,

    ChiefSecurityOfficer = 188,
    ChiefTechnicalOfficer = 189,
    ChiefProductOfficer = 190,
    ChiefMarketingOfficer = 191,
    ChiefBusinessDevelopmentOfficer = 192,
    ChiefBusinessOfficer = 193,
    ChiefCommercialOfficer = 194,
    ChiefTechnologyOfficer = 195,
    ChiefLegalOfficer = 196,
    ChiefFinancialOfficer = 197,
    ChiefHumanResourcesOfficer = 198,
    ChiefExecutiveOfficer = 199,

    BoardOfDirectorsMember = 297,
    BoardOfDirectorsViceChairman = 298,
    BoardOfDirectorsChairman = 299,

    CoFounder = 398,
    Founder = 399
}
