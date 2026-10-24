namespace ERP.Domain.Entities.Modules.ProductionManagement;

public sealed partial class Routing
{
    public void SetRoutingOperationQueueHours(Guid childId, decimal queueHours)
    {
        EnsureDraft();
        var child = GetChild(childId, _operations);
        child.SetQueueHours(queueHours);
    }

    public void SetRoutingOperationScrapPercentage(Guid childId, Percentage scrapPercentage)
    {
        EnsureDraft();
        var child = GetChild(childId, _operations);
        child.SetScrapPercentage(scrapPercentage);
    }

    public void RequireRoutingOperationQualityCheck(Guid childId)
    {
        EnsureDraft();
        var child = GetChild(childId, _operations);
        child.RequireQualityCheck();
    }

    public void SetRoutingOperationInstructions(Guid childId, string? instructions)
    {
        EnsureDraft();
        var child = GetChild(childId, _operations);
        child.SetInstructions(instructions);
    }
}
