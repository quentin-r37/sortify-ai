using Avalonia.Controls.Notifications;

namespace FileOrganizer.UIService;

public class NotificationService(INotificationManager notificationManager) : INotificationService
{
    public void ShowNotification(string title, string message, NotificationType notificationType)
    {
        notificationManager.Show(new Notification(title, message, notificationType));
    }
}