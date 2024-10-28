using Avalonia.Controls.Notifications;

namespace FileOrganizer.UIService;

public interface INotificationService
{
    void ShowNotification(string title, string message, NotificationType notificationType);
}