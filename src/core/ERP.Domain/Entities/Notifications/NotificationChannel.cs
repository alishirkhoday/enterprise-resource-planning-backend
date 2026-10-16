namespace ERP.Domain.Entities.Notifications;

/// <summary>
/// How a notification reaches somebody.
/// </summary>
public enum NotificationChannel
{
    ShortMessageService = 0,
    Email = 1,
    InApp = 2,
    PushNotification = 3,
    Webhook = 4
}
