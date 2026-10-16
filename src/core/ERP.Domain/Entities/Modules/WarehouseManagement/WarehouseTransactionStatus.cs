namespace ERP.Domain.Entities.Modules.WarehouseManagement;

/// <summary>
/// Whether a warehouse document has actually moved anything yet.
/// </summary>
public enum WarehouseTransactionStatus
{
    Draft = 0,
    Definite = 1,
    SentToAccounting = 2
}
