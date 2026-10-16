namespace ERP.Domain.Entities.Modules.AfterSalesServiceManagement;

/// <summary>
/// How far a service agreement reaches. It decides what a visit may bill: under a full agreement
/// the parts are already paid for, under a labour-only one they are not.
/// </summary>
public enum ServiceCoverageType
{
    /// <summary>Labour and travel, parts charged separately.</summary>
    LabourOnly = 0,

    /// <summary>Labour and parts, everything included.</summary>
    Comprehensive = 1,

    /// <summary>Scheduled preventive visits only; breakdowns are charged.</summary>
    PreventiveMaintenance = 2,

    /// <summary>Remote diagnosis and advice, no site visit included.</summary>
    RemoteSupport = 3,

    /// <summary>A replacement unit while the original is repaired.</summary>
    ReplacementUnit = 4
}
