namespace ERP.Domain.Entities.Common;

public sealed partial class WorkCalendar
{
    public void UpdateHoliday(Guid childId, string title, bool isOfficial)
    {
        var child = GetChild(childId, _holidays);
        child.Update(title, isOfficial);
    }

    public void SetHolidayDescription(Guid childId, string? description)
    {
        var child = GetChild(childId, _holidays);
        child.SetDescription(description);
    }
}
