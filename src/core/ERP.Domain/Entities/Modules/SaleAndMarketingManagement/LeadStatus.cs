namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// Where a lead has got to. Converted and disqualified both close it, but only one of them
/// means the duplicate check should stop blocking the same person coming back.
/// </summary>
public enum LeadStatus
{
    New = 0,
    Contacted = 1,
    Qualified = 2,
    Disqualified = 3,

    /// <summary>Became a customer.</summary>
    Converted = 4
}
