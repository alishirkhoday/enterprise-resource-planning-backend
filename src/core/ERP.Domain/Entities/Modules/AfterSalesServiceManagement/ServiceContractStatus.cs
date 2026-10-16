namespace ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

/// <summary>
/// Where a service agreement has got to. Only an active contract entitles a customer to
/// anything.
/// </summary>
public enum ServiceContractStatus
{
    Draft = 0,
    Active = 1,
    Expired = 2,
    Terminated = 3
}
