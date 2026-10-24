namespace ERP.Domain.Entities.Modules.ProjectManagement;

public sealed partial class Timesheet
{
    public void SetTimesheetEntryBillingRate(Guid childId, Money billingRate)
    {
        EnsureEditable();
        var child = GetChild(childId, _entries);
        child.SetBillingRate(billingRate);
    }

    public void MarkTimesheetEntryAsNonBillable(Guid childId)
    {
        EnsureEditable();
        var child = GetChild(childId, _entries);
        child.MarkAsNonBillable();
    }

    public void AssignTimesheetEntryCostCenter(Guid childId, Guid costCenterId)
    {
        EnsureEditable();
        var child = GetChild(childId, _entries);
        child.AssignCostCenter(costCenterId);
    }

    public void SetTimesheetEntryDescription(Guid childId, string? description)
    {
        EnsureEditable();
        var child = GetChild(childId, _entries);
        child.SetDescription(description);
    }
}
