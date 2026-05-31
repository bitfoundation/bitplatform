using System;
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
        return await js.Invoke<bool>("BitButil.notification.isSupported");
    }

    /// <summary>
    /// Gets the current permission of the Notification API.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Notification/permission_static">https://developer.mozilla.org/en-US/docs/Web/API/Notification/permission_static</see>
    /// </summary>
    public async ValueTask<NotificationPermission> GetPermission()
    {
        var permission = await js.Invoke<string>("BitButil.notification.getPermission");

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
        var permission = await js.Invoke<string>("BitButil.notification.requestPermission");

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

        await js.InvokeVoid("BitButil.notification.show", title, opts);
    }

    /// <summary>
    /// Shows a notification and returns a <see cref="NotificationHandle"/> that lets you wire up
    /// click / show / close / error callbacks and close the toast programmatically. The notification
    /// stays open until the user dismisses it (or you call <see cref="NotificationHandle.Close"/>).
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NotificationOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InternalNotificationOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NotificationListenersManager))]
    public async ValueTask<NotificationHandle> ShowTracked(string title,
                                                           NotificationOptions? options = null,
                                                           Action? onClick = null,
                                                           Action? onShow = null,
                                                           Action? onClose = null,
                                                           Action? onError = null)
    {
        var listener = new NotificationListenersManager.Listener
        {
            OnClick = onClick,
            OnShow = onShow,
            OnClose = onClose,
            OnError = onError
        };
        var id = NotificationListenersManager.Add(listener);

        InternalNotificationOptions? opts = options is null ? null : new(options);

        await js.InvokeVoid("BitButil.notification.showTracked",
            id,
            title,
            opts,
            NotificationListenersManager.ClickMethodName,
            NotificationListenersManager.ShowMethodName,
            NotificationListenersManager.CloseMethodName,
            NotificationListenersManager.ErrorMethodName);

        return new NotificationHandle(js, id);
    }
}
