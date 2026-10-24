using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Domain.Entities.Modules.ProductionManagement;

public sealed partial class ProductionOrder
{
    public void StartTaskAssignment(Guid childId)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks.SelectMany(parent => parent.Assignments));
        child.Start();
    }

    public void EndTaskAssignment(Guid childId, decimal hours)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks.SelectMany(parent => parent.Assignments));
        child.End(hours);
    }

    public void SetTaskAssignmentNotes(Guid childId, string? notes)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks.SelectMany(parent => parent.Assignments));
        child.SetNotes(notes);
    }

    public void ScheduleProductionTask(Guid childId, DateTimeOffset plannedStartTime, DateTimeOffset plannedEndTime)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks);
        child.Schedule(plannedStartTime, plannedEndTime);
    }

    public void LinkProductionTaskToRoutingOperation(Guid childId, Guid routingOperationId)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks);
        child.LinkToRoutingOperation(routingOperationId);
    }

    public void AssignProductionTaskResources(Guid childId, Guid? workStationId, Guid? machineId)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks);
        child.AssignResources(workStationId, machineId);
    }

    public void AddProductionTaskAssignment(Guid childId, TaskAssignment assignment)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks);
        child.AddAssignment(assignment);
    }

    public void StartProductionTask(Guid childId)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks);
        child.Start();
    }

    public void RecordProductionTaskProgress(Guid childId, decimal completedQuantity, decimal scrappedQuantity = 0m)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks);
        child.RecordProgress(completedQuantity, scrappedQuantity);
    }

    public void RecordProductionTaskHours(Guid childId, decimal hours)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks);
        child.RecordHours(hours);
    }

    public void PauseProductionTask(Guid childId, string reason)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks);
        child.Pause(reason);
    }

    public void ResumeProductionTask(Guid childId)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks);
        child.Resume();
    }

    public void CompleteProductionTask(Guid childId)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks);
        child.Complete();
    }

    public void CancelProductionTask(Guid childId, string reason)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks);
        child.Cancel(reason);
    }

    public void SetProductionTaskNotes(Guid childId, string? notes)
    {
        EnsureChildChangesAllowed();
        var child = GetChild(childId, _tasks);
        child.SetNotes(notes);
    }
    private void EnsureChildChangesAllowed()
    {
        if (Status is ProductionOrderStatus.Completed or ProductionOrderStatus.Cancelled)
            throw new InvalidOperationException("A completed or cancelled aggregate cannot change its children.");
    }
}
