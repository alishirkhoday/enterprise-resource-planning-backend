namespace ERP.Domain.Entities.Modules.ProductionManagement;

/// <summary>
/// Where a routing has got to - the sequence of operations, alongside the bill that says what
/// goes in.
/// </summary>
public enum RoutingStatus
{
    Draft = 0,
    Approved = 1,

    /// <summary>Superseded; kept only so old orders remain explainable.</summary>
    Obsolete = 2
}
