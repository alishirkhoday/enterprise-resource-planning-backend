namespace ERP.Domain.Entities.Modules.WarehouseManagement;

/// <summary>
/// What a warehouse is for. Decides what may be stored in it and whether its stock counts as
/// available to sell.
/// </summary>
public enum WarehouseType
{
    /// <summary>
    /// مواد اولیه اصلی
    /// </summary>
    RawMaterial = 0,
    /// <summary>
    /// نیمه ساخته
    /// </summary>
    SemiFinished = 1,
    /// <summary>
    /// محصول نهایی
    /// </summary>
    Product = 2,
    /// <summary>
    /// تعمیر و نگهداری
    /// </summary>
    Maintenance = 3,
    /// <summary>
    /// ملزومات مصرفی
    /// </summary>
    Consumable = 4,
    /// <summary>
    /// لوازم بسته‌بندی
    /// </summary>
    Packaging = 5,
    /// <summary>
    /// مواد بازیافتی
    /// </summary>
    Recycle = 6,
    /// <summary>
    /// ضایعات
    /// </summary>
    Scrap = 7,
    /// <summary>
    /// انبار در مسیر
    /// </summary>
    Transit = 8
}
