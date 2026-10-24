namespace ERP.Domain.Entities.Modules.ProductionManagement;

public sealed partial class QualityControlCheck
{
    public void SetQualityMeasurementNotes(Guid childId, string? notes)
    {
        var child = GetChild(childId, _measurements);
        child.SetNotes(notes);
    }
}
