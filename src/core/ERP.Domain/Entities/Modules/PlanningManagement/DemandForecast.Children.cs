using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Domain.Entities.Modules.PlanningManagement;

public sealed partial class DemandForecast
{
    public void SetDemandForecastLineConfidence(Guid childId, Percentage confidence)
    {
        EnsureDraft();
        var child = GetChild(childId, _lines);
        child.SetConfidence(confidence);
    }

    public void SetDemandForecastLineDescription(Guid childId, string? description)
    {
        EnsureDraft();
        var child = GetChild(childId, _lines);
        child.SetDescription(description);
    }

    public void RecordDemandForecastLineActual(Guid childId, decimal actualQuantity)
    {
        var child = GetChild(childId, _lines);
        child.RecordActual(actualQuantity);
    }
}
