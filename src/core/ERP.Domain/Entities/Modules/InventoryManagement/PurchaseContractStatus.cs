namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Where a supply agreement has got to. Only an active contract prices an order.
/// </summary>
public enum PurchaseContractStatus
{
    Draft = 0,
    Active = 1,
    Expired = 2,
    Terminated = 3
}
