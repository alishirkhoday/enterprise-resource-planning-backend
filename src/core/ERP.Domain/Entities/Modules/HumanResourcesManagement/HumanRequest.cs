namespace ERP.Domain.Entities.Modules.HumanResourcesManagement;

/// <summary>
/// Something an employee asks for and a manager decides - درخواست: leave, remote work, a
/// mission, a forgotten clock-in.
/// 
/// Days are held against the entitlement when the request is made rather than when it is
/// approved. Otherwise the same week can be booked repeatedly while the first request sits
/// in a queue.
/// </summary>
public sealed class HumanRequest : AggregateRoot
{
    public Human Human { get; private set; } = default!;
    public Guid HumanId { get; private set; }
    public HumanRequestType Type { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public HumanRequestLeaveType? LeaveType { get; private set; }
    public string? Reason { get; private set; }
    public HumanRequestStatus Status { get; private set; }

    private HumanRequest() { }

    public HumanRequest(Human human, HumanRequestType type, DateOnly startDate, DateOnly endDate, TimeOnly? startTime = null, TimeOnly? endTime = null, HumanRequestLeaveType? leaveType = null, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(human, nameof(human));
        Human = human;
        SetDateTimeByType(type, startDate, endDate, startTime, endTime, leaveType, reason);
        Status = HumanRequestStatus.Pending;
    }

    /// <summary>
    /// Only a pending request can be decided. Without that guard an approval could be recorded
    /// twice, or over a cancellation, and each pass would announce the leave again.
    /// </summary>
    public void SetApprove()
    {
        EnsurePending();
        Status = HumanRequestStatus.Approved;

        if (Type is HumanRequestType.LeaveDaily or HumanRequestType.LeaveHourly)
            Raise(new LeaveRequestApproved(Id, HumanId, StartDate, EndDate));
    }

    public void SetReject()
    {
        EnsurePending();
        Status = HumanRequestStatus.Rejected;
    }

    public void SetCancel()
    {
        if (Status == HumanRequestStatus.Cancelled)
            return;
        Status = HumanRequestStatus.Cancelled;
    }

    private void EnsurePending()
    {
        if (Status != HumanRequestStatus.Pending)
            throw new InvalidOperationException($"A request in state {Status} can no longer be decided.");
    }

    private void SetDateTimeByType(HumanRequestType type, DateOnly startDate, DateOnly endDate, TimeOnly? startTime = null, TimeOnly? endTime = null, HumanRequestLeaveType? leaveType = null, string? reason = null)
    {
        if (type == HumanRequestType.LeaveDaily)
        {
            StartDate = startDate;
            EndDate = endDate;
            if (leaveType == HumanRequestLeaveType.Other)
            {
                Reason = reason?.Trim();
            }
            LeaveType = leaveType;
        }
        else if (type == HumanRequestType.LeaveHourly)
        {
            StartDate = startDate;
            EndDate = endDate;
            StartTime = startTime;
            EndTime = endTime;
            if (leaveType == HumanRequestLeaveType.Other)
            {
                Reason = reason?.Trim();
            }
            LeaveType = leaveType;
        }
        else if (type == HumanRequestType.RemoteWork)
        {
            StartDate = startDate;
            EndDate = endDate;
            StartTime = startTime;
            EndTime = endTime;
        }
        else if (type == HumanRequestType.MissionWork)
        {
            StartDate = startDate;
            EndDate = endDate;
            StartTime = startTime;
            EndTime = endTime;
        }
        else if (type == HumanRequestType.ForgetAttendanceRecord)
        {
            StartDate = startDate;
            EndDate = endDate;
            StartTime = startTime;
            EndTime = endTime;
        }
        else
        {
            throw new ArgumentException("type is invalid");
        }
        Type = type;
    }
}
