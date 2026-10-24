namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

public sealed partial class JobApplication
{
    public void SetInterviewLocation(Guid childId, string? location)
    {
        EnsureOpen();
        var child = GetChild(childId, _interviews);
        child.SetLocation(location);
    }

    public void AssignInterviewInterviewer(Guid childId, Guid humanId)
    {
        EnsureOpen();
        var child = GetChild(childId, _interviews);
        child.AssignInterviewer(humanId);
    }

    public void RescheduleInterview(Guid childId, DateTimeOffset scheduledFor)
    {
        var child = GetChild(childId, _interviews);
        child.Reschedule(scheduledFor);
    }

    public void RecordInterviewResult(Guid childId, InterviewResult result, int? score, string? feedback)
    {
        var child = GetChild(childId, _interviews);
        child.RecordResult(result, score, feedback);
    }
}
