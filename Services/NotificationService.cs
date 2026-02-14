using Microsoft.Toolkit.Uwp.Notifications;

namespace TimeMaker.Services
{
    public class NotificationService
    {
        public static void ShowInfoNotification(string title, string message)
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .SetToastDuration(ToastDuration.Short)
                .Show(toast =>
                {
                    toast.ExpirationTime = DateTimeOffset.Now.AddSeconds(1);
                });
        }
    }
}