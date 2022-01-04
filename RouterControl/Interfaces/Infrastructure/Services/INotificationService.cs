using RouterControl.Infrastructure.Enums;

namespace RouterControl.Interfaces.Infrastructure.Services
{
    internal interface INotificationService
    {
        NotificationResult Notify(string text, string caption, NotificationButtons notificationButton = NotificationButtons.Ok,
            NotificationImages notificationImage = NotificationImages.None);
    }
}