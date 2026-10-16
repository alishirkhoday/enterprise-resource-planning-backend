namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Which barcode standard a code is printed in, which decides how a scanner reads it.
/// </summary>
public enum BarcodeSymbology
{
    Ean13 = 0,
    Ean8 = 1,
    UpcA = 2,
    Code128 = 3,
    Code39 = 4,
    QrCode = 5,
    DataMatrix = 6,
    Internal = 99
}
