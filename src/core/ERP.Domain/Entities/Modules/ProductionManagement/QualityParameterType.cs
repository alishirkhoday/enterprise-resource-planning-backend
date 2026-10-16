namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Whether a quality parameter is measured or judged. A measured one is checked against its
/// own tolerance and cannot be talked into passing; a judged one rests on the inspector.
/// </summary>
public enum QualityParameterType
{
    /// <summary>Measured on a scale, against a tolerance.</summary>
    Numeric = 0,

    /// <summary>Judged by eye or by feel.</summary>
    Visual = 1,

    /// <summary>Either present or not.</summary>
    Boolean = 2
}
