namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// Where an instalment arrangement has got to - فروش اقساطی.
/// </summary>
public enum InstallmentPlanStatus
{
    Draft = 0,
    Active = 1,
    Settled = 2,

    /// <summary>The customer has stopped paying.</summary>
    Defaulted = 3,

    Cancelled = 4
}
