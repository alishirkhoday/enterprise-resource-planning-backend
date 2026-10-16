namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// What kind of thing an item is - مواد اولیه، محصول، قطعه و غیره.
/// </summary>
public enum ItemType
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
    /// قطعات یدکی
    /// </summary>
    Part = 3,
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
    /// خدمات
    /// </summary>
    Service = 8
}
