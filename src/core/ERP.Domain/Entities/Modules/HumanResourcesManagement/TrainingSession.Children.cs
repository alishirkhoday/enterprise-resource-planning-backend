namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

public sealed partial class TrainingSession
{
    public void RecordTrainingEnrollmentAttendance(Guid childId, decimal attendedHours)
    {
        var child = GetChild(childId, _enrollments);
        child.RecordAttendance(attendedHours);
    }

    public void RecordTrainingEnrollmentResult(Guid childId, int score, int passingScore = 60)
    {
        var child = GetChild(childId, _enrollments);
        child.RecordResult(score, passingScore);
    }

    public void IssueCertificateTrainingEnrollment(Guid childId, string certificateNumber, DateOnly issuedOn, int? validityMonths)
    {
        var child = GetChild(childId, _enrollments);
        child.IssueCertificate(certificateNumber, issuedOn, validityMonths);
    }

    public void MarkTrainingEnrollmentAsAbsent(Guid childId)
    {
        var child = GetChild(childId, _enrollments);
        child.MarkAsAbsent();
    }

    public void CancelTrainingEnrollment(Guid childId)
    {
        var child = GetChild(childId, _enrollments);
        child.Cancel();
    }
}
