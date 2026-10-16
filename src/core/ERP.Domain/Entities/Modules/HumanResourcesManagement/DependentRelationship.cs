namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// How a dependant is related to the employee - افراد تحت تکفل. Drives insurance and some
/// allowances.
/// </summary>
public enum DependentRelationship
{
    Spouse = 0,
    Child = 1,
    Father = 2,
    Mother = 3,
    Sibling = 4,
    Other = 99
}
