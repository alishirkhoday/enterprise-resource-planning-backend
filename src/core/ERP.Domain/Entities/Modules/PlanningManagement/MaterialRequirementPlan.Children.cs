using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.PlanningManagement;

public sealed partial class MaterialRequirementPlan
{
    public void SetMaterialRequirementPlanLineSafetyQuantity(Guid childId, decimal safetyQuantity)
    {
        var child = GetChild(childId, _lines);
        child.SetSafetyQuantity(safetyQuantity);
    }

    public void TraceMaterialRequirementPlanLineToParent(Guid childId, Guid parentLineId, short billOfMaterialsLevel)
    {
        var child = GetChild(childId, _lines);
        child.TraceToParent(parentLineId, billOfMaterialsLevel);
    }

    public void SuggestMaterialRequirementPlanLine(Guid childId, PlannedOrderAction action, decimal plannedOrderQuantity, int leadTimeDays, Guid? supplierId = null)
    {
        var child = GetChild(childId, _lines);
        child.Suggest(action, plannedOrderQuantity, leadTimeDays, supplierId);
    }

    public void SetMaterialRequirementPlanLineDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _lines);
        child.SetDescription(description);
    }

    public void MarkMaterialRequirementPlanLineAsReleased(Guid childId, string referenceType, string referenceId)
    {
        var child = GetChild(childId, _lines);
        child.MarkAsReleased(referenceType, referenceId);
    }
}
