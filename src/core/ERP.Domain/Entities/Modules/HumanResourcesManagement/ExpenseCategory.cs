namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// What a claimed expense was for, which decides the account it is posted to and the limit
/// it is checked against.
/// </summary>
public enum ExpenseCategory
{
    Transportation = 0,
    Accommodation = 1,
    Meals = 2,
    Fuel = 3,
    Communication = 4,
    Entertainment = 5,
    OfficeSupplies = 6,
    Other = 99
}
