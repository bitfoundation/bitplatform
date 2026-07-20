namespace Bit.Butil;

public static class NotificationExtensions
{
    extension(Notification notification)
    {
        public async Task<bool> IsNotificationAvailable()
        {
            var isPresent = await notification.IsSupported();
            if (isPresent)
            {
                if (await notification.GetPermission() is NotificationPermission.Granted)
                    return true;
            }
            return false;
        }
    }
}
