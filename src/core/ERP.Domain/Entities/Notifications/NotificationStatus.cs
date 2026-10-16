namespace ERP.Domain.Entities.Notifications;

/// <summary>
/// Where a notification has got to on its way to a person.
/// </summary>
public enum NotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    Read = 3,
    Cancelled = 4
}
