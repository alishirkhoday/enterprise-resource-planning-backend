namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Where a recipe has got to. Only an approved bill in its validity window can be attached to
/// a new works order; obsolete ones stay because old orders were costed against them.
/// </summary>
public enum BillOfMaterialsStatus
{
    Draft = 0,
    Approved = 1,
    Obsolete = 2
}
