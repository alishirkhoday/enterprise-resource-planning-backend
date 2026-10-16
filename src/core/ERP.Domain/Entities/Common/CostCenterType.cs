namespace ERP.Domain.Entities.Common;

/// <summary>
/// What kind of cost centre this is, which decides how its costs are eventually absorbed.
/// </summary>
public enum CostCenterType
{
    /// <summary>Directly produces output, such as a production line.</summary>
    Production = 0,

    /// <summary>Supports production without producing output, such as maintenance.</summary>
    Service = 1,

    /// <summary>Administrative overhead, such as finance or human resources.</summary>
    Administrative = 2,

    Sales = 3,
    Project = 4,
    Other = 99
}
