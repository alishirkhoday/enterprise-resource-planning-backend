using ERP.Domain.Entities.Modules.ProductionManagement;

namespace ERP.Domain.Entities.Modules.PlanningManagement;

public sealed partial class CapacityPlan
{
    public void SetCapacityPlanLineEfficiency(Guid childId, Percentage efficiency)
    {
        var child = GetChild(childId, _lines);
        child.SetEfficiency(efficiency);
    }

    public void RecordCapacityPlanLineLoad(Guid childId, decimal firmLoadHours, decimal plannedLoadHours)
    {
        var child = GetChild(childId, _lines);
        child.RecordLoad(firmLoadHours, plannedLoadHours);
    }

    public void SetCapacityPlanLineDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _lines);
        child.SetDescription(description);
    }
}
