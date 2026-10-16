namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// The basis somebody is engaged on - تمام‌وقت، پاره‌وقت و غیره. Decides how their pay and
/// their entitlements are worked out.
/// </summary>
public enum HumanEmploymentType
{
    FullTime = 0,
    PartTime = 1,
    Internship = 2,
    Freelance = 3,
    Seasonal = 4
}
