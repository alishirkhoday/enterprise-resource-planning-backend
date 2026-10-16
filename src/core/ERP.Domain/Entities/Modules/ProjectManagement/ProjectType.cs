namespace ERP.Domain.Entities.Modules.ProjectManagement;

/// <summary>
/// What kind of project it is, which decides how it is billed and costed.
/// </summary>
public enum ProjectType
{
    /// <summary>Delivered for a customer against a contract.</summary>
    Customer = 0,

    /// <summary>Run for the organization itself.</summary>
    Internal = 1,

    /// <summary>A capital project that ends in a fixed asset.</summary>
    Capital = 2,

    ResearchAndDevelopment = 3,
    Maintenance = 4
}
