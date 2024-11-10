using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// This service is used to configure and display desktop notifications to the user.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Notification">https://developer.mozilla.org/en-US/docs/Web/API/Notification</see>
/// </summary>
public class Notification(IJSRuntime js)
{
    /// <summary>
    /// Requests a native notification to show to the user.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Notification/Notification">https://developer.mozilla.org/en-US/docs/Web/API/Notification/Notification</see>
    /// </summary>
    public async ValueTask Show(string title, NotificationOptions? options)
    {
        object? opts = null;
        if (options is not null)
        {
            opts = new
            {
                badge = options.Badge,
                body = options.Body,
                data = options.Data,
                dir = options.Dir.ToString().ToLower(),
                icon = options.Icon,
                image = options.Image,
                lang = options.Lang,
                renotify = options.Renotify,
                requireInteraction = options.RequireInteraction,
                silent = options.Silent,
                tag = options.Tag,
                timestamp = options.Timestamp,
                vibrate = options.Vibrate,
            };
        }

        await js.InvokeVoidAsync("BitButil.notification.show", title, opts);
    }

    /// <summary>
    /// Gets the current permission of the Notification API.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Notification/permission_static">https://developer.mozilla.org/en-US/docs/Web/API/Notification/permission_static</see>
    /// </summary>
    public async ValueTask<NotificationPermission> GetPermission()
    {
        var permission = await js.InvokeAsync<string>("BitButil.notification.getPermission");

        return permission switch
        {
            "denied" => NotificationPermission.Denied,
            "granted" => NotificationPermission.Granted,
            "default" => NotificationPermission.Default,
            _ => NotificationPermission.Default
        };
    }

    /// <summary>
    /// Requests permission from the user for the current origin to display notifications.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Notification/requestPermission_static">https://developer.mozilla.org/en-US/docs/Web/API/Notification/requestPermission_static</see>
    /// </summary>
    public async ValueTask<NotificationPermission> RequestPermission()
    {
        var permission = await js.InvokeAsync<string>("BitButil.notification.requestPermission");

        return permission switch
        {
            "denied" => NotificationPermission.Denied,
            "granted" => NotificationPermission.Granted,
            "default" => NotificationPermission.Default,
            _ => NotificationPermission.Default
        };
    }
}
