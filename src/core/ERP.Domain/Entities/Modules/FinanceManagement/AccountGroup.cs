namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// How deep in the chart an account sits - کل، معین، تفصیلی. Depth decides what may be
/// posted to it and how a report groups it.
/// </summary>
public enum AccountGroup
{
    /// <summary>
    /// کل
    /// </summary>
    LevelOne = 0,
    /// <summary>
    /// معین
    /// </summary>
    LevelTwo = 1,
    /// <summary>
    /// تفضیل
    /// </summary>
    LevelThree = 2,
    /// <summary>
    /// ریز
    /// </summary>
    LevelFour = 3,
    /// <summary>
    /// جزء
    /// </summary>
    LevelFive = 4
}
