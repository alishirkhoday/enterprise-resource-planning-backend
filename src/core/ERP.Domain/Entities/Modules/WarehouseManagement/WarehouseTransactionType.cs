namespace ERP.Domain.Entities.Modules.WarehouseManagement;

/// <summary>
/// What kind of movement a warehouse document records.
/// </summary>
public enum WarehouseTransactionType
{
    /// <summary>
    /// ورود به انبار
    /// </summary>
    Receipt = 1,
    /// <summary>
    /// خروج از انبار
    /// </summary>
    Remittance = 2,
    /// <summary>
    /// انتقال بین انبارهای داخل سازمان
    /// </summary>
    Transfer = 3
}
