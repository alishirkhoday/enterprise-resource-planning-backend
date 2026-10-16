namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Whether the work was planned or a response to something breaking - نگهداری پیشگیرانه
/// یا اصلاحی. The ratio between the two is the usual measure of how well maintenance is run.
/// </summary>
public enum MaintenanceType
{
    /// <summary>Planned work done to stop a failure happening.</summary>
    Preventive = 0,

    /// <summary>Work done after something has already broken.</summary>
    Corrective = 1,

    /// <summary>Work triggered by a measured condition rather than a calendar.</summary>
    Predictive = 2,

    Inspection = 3,
    Calibration = 4,
    Overhaul = 5
}
