namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Where a request has got to. Cancelled and rejected both release the days that were held,
/// but only one of them was the employee's own doing.
/// </summary>
public enum HumanRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}
