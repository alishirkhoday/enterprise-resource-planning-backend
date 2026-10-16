namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// How an asset loses value over time - روش استهلاک. None is a real choice, for land and
/// for assets held at cost.
/// </summary>
public enum DepreciationMethod
{
    /// <summary>Equal charge every period over the useful life.</summary>
    StraightLine = 0,

    /// <summary>A fixed rate applied to the shrinking book value.</summary>
    DecliningBalance = 1,

    /// <summary>Charge proportional to output rather than to time.</summary>
    UnitsOfProduction = 2,

    /// <summary>Not depreciated at all, as with land.</summary>
    None = 99
}
