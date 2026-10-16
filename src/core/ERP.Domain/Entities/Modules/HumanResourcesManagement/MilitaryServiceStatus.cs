namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Military service standing - وضعیت نظام وظیفه. Required on Iranian employment records.
/// </summary>
public enum MilitaryServiceStatus
{
    Completed = 0,
    PermanentExemption = 1,
    MedicalExemption = 2,
    EducationalDeferment = 3,
    CurrentlyServing = 4,
    NotApplicable = 99
}
