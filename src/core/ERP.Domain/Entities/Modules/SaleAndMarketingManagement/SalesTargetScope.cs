namespace ERP.Domain.Entities.Modules.SaleAndMarketingManagement;

/// <summary>
/// Who a quota belongs to - a person, a team, a territory, a branch or the whole company.
/// </summary>
public enum SalesTargetScope
{
    SalesRepresentative = 0,
    Team = 1,
    Territory = 2,
    Branch = 3,
    Organization = 4
}
