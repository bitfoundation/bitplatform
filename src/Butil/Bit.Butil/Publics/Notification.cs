using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// This service is used to configure and display native notifications to the user.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Notification">https://developer.mozilla.org/en-US/docs/Web/API/Notification</see>
/// </summary>
public class Notification(IJSRuntime js)
{
    /// <summary>
    /// Checks if the runtime (browser or web-view) is supporting the Web Notification API.
    /// </summary>
    public async ValueTask<bool> IsSupported()
    {
        return await js.FastInvokeAsync<bool>("BitButil.notification.isSupported");
    }

    /// <summary>
    /// Gets the current permission of the Notification API.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Notification/permission_static">https://developer.mozilla.org/en-US/docs/Web/API/Notification/permission_static</see>
    /// </summary>
    public async ValueTask<NotificationPermission> GetPermission()
    {
        var permission = await js.FastInvokeAsync<string>("BitButil.notification.getPermission");

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

    /// <summary>
    /// Requests a native notification to show to the user.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Notification/Notification">https://developer.mozilla.org/en-US/docs/Web/API/Notification/Notification</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NotificationOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InternalNotificationOptions))]
    public async ValueTask Show(string title, NotificationOptions? options = null)
    {
        InternalNotificationOptions? opts = null;
        if (options is not null)
        {
            opts = new(options);
        }

        await js.FastInvokeVoidAsync("BitButil.notification.show", title, opts);
    }
}
