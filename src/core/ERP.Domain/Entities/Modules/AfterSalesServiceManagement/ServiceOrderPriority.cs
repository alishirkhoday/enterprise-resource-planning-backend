namespace ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

/// <summary>
/// How urgent a service visit is, normally taken from what the contract promises.
/// </summary>
public enum ServiceOrderPriority
{
    Low = 0,
    Normal = 1,
    High = 2,

    /// <summary>The customer's operation is stopped.</summary>
    Critical = 3
}
