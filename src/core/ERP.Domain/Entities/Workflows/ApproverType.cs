namespace ERP.Domain.Entities.Workflows;

/// <summary>
/// How the approver for a step is chosen - a named person, a role, or whoever manages the
/// person who raised it.
/// </summary>
public enum ApproverType
{
    /// <summary>Anyone holding a given role.</summary>
    Role = 0,

    /// <summary>Anyone occupying a given position in the organization chart.</summary>
    Position = 1,

    /// <summary>One named user.</summary>
    SpecificUser = 2,

    /// <summary>The direct manager of whoever raised the request.</summary>
    DirectManager = 3,

    /// <summary>The head of the department the request originated from.</summary>
    DepartmentHead = 4
}
