namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// What the interviewers concluded.
/// </summary>
public enum InterviewResult
{
    Passed = 0,
    Failed = 1,
    OnHold = 2,
    CandidateDidNotAttend = 3
}
