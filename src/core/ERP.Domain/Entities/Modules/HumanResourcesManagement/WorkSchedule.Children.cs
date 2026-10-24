namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

public sealed partial class WorkSchedule
{
    public void MarkWorkScheduleDayAsRestDay(Guid childId)
    {
        var child = GetChild(childId, _days);
        child.MarkAsRestDay();
    }
}
