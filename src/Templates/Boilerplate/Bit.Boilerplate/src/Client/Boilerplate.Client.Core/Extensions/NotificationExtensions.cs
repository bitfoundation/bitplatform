namespace Bit.Butil;

public static class NotificationExtensions
{
    public static async Task<bool> IsNotificationAvailable(this Notification notification)
    {
        var isPresent = await notification.IsSupported();
        if (isPresent)
        {
            if (await notification.GetPermission() is NotificationPermission.Granted)
                return true;
            return await notification.RequestPermission() is NotificationPermission.Granted;
        }
        return false;
    }
}
