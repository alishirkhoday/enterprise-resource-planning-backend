namespace ERP.Domain.Entities.Notifications;

/// <summary>
/// How urgent a notification is, which decides whether it interrupts or waits in a list.
/// </summary>
public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}
