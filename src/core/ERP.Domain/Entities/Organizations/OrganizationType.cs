namespace ERP.Domain.Entities.Organizations;

/// <summary>
/// What legal form the organisation takes, which decides what tax identifiers it must carry.
/// </summary>
public enum OrganizationType
{
    PrivateJointStock = 0,
    PublicJointStock = 1,
    LimitedLiability = 2,
    Cooperative = 3,
    SoleProprietorship = 4,
    GovernmentalEntity = 5,
    NonProfit = 6,
    Other = 99
}
