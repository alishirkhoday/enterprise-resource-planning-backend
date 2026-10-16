namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// What a budget governs - operating spend, capital, cash, revenue or one project.
/// </summary>
public enum BudgetType
{
    Operating = 0,
    Capital = 1,
    Cash = 2,
    Revenue = 3,
    Project = 4
}
