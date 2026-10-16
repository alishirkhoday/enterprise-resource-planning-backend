namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// Whether the tax was charged to a customer or paid to a supplier. A return nets the two,
/// so a line that does not know its direction cannot be reported.
/// </summary>
public enum TaxDirection
{
    /// <summary>Tax charged to a customer on a sale, owed to the authority.</summary>
    Output = 0,

    /// <summary>Tax paid to a supplier on a purchase, reclaimable from the authority.</summary>
    Input = 1
}
