namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// What kind of insurance a policy or a payroll deduction is for - بیمه.
/// </summary>
public enum InsuranceType
{
    /// <summary>The state social security scheme.</summary>
    SocialSecurity = 0,

    /// <summary>Supplementary health cover bought from a private insurer.</summary>
    SupplementaryHealth = 1,

    /// <summary>Cover against workplace accidents.</summary>
    Accident = 2,

    LifeInsurance = 3,
    Other = 99
}
