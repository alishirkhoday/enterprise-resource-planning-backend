namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// What kind of overtime was worked - اضافه‌کاری. Iranian law pays these at different
/// multiples, so the kind is what decides the rate.
/// </summary>
public enum OvertimeType
{
    /// <summary>Extra hours on an ordinary working day.</summary>
    Ordinary = 0,

    /// <summary>Hours worked on a weekend.</summary>
    Weekend = 1,

    /// <summary>Hours worked on an official holiday.</summary>
    Holiday = 2,

    /// <summary>Hours worked during the night shift window.</summary>
    NightShift = 3
}
