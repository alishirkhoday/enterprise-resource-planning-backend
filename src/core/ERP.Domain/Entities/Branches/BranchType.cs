namespace ERP.Domain.Entities.Branches;

/// <summary>
/// What kind of place a branch is - a shop, a warehouse, a head office.
/// </summary>
public enum BranchType
{
    HeadOffice = 0,
    Store = 1,
    Warehouse = 2,
    Factory = 3,
    SalesOffice = 4,
    ServiceCenter = 5,
    Other = 99
}
