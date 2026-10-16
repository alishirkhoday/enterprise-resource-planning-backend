namespace ERP.Domain.Entities.Modules.InventoryManagement;

/// <summary>
/// Whether the supplier is a person or an organisation, which decides the tax identifiers
/// they must carry.
/// </summary>
public enum SupplierType
{
    Individual = 0,
    Organization = 1
}
