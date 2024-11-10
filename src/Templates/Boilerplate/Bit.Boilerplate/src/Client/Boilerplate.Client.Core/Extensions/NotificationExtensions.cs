// Temporary code

using System.Reflection;

namespace Bit.Butil;

public static class NotificationExtensions
{
    public static async Task<bool> IsSupported(this Notification notification)
    {
        var js = (IJSRuntime)typeof(Notification).GetField("<js>P", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(notification)!;
        var isPresent = await js.InvokeAsync<bool>("window.hasOwnProperty", "Notification");
        if (isPresent)
        {
            if (await notification.GetPermission() is NotificationPermission.Granted)
                return true;
            return await notification.RequestPermission() is NotificationPermission.Granted;
        }
        return false;
    }
}
