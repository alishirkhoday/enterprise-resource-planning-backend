namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// What kind of loan it is, which decides how its instalments are worked out.
/// </summary>
public enum LoanType
{
    /// <summary>A general purpose advance against future pay.</summary>
    SalaryAdvance = 0,

    Housing = 1,
    Marriage = 2,
    Medical = 3,
    Education = 4,
    Emergency = 5,
    Other = 99
}
