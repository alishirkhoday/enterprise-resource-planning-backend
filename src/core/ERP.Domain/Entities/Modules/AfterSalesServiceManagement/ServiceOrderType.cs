namespace ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

/// <summary>
/// Who is paying for the visit. Decided when the order is raised, because it governs whether the
/// parts and labour ever reach an invoice.
/// </summary>
public enum ServiceOrderType
{
    /// <summary>The customer pays.</summary>
    Chargeable = 0,

    /// <summary>Covered by warranty on the original sale.</summary>
    Warranty = 1,

    /// <summary>Covered by a service contract.</summary>
    Contract = 2,

    /// <summary>A scheduled preventive visit.</summary>
    PreventiveMaintenance = 3,

    /// <summary>Not owed, but given. Recorded as such so the cost of goodwill is visible.</summary>
    Goodwill = 4,

    /// <summary>Putting right this organization's own earlier mistake.</summary>
    Rework = 5
}
