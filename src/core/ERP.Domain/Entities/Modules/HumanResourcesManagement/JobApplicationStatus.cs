namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Where one candidate has got to in a hiring process.
/// </summary>
public enum JobApplicationStatus
{
    Received = 0,
    Screening = 1,
    Shortlisted = 2,
    Interviewing = 3,
    OfferMade = 4,
    Hired = 5,
    Rejected = 6,
    Withdrawn = 7
}
