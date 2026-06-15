using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// This service is used to configure and display native notifications to the user.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Notification">https://developer.mozilla.org/en-US/docs/Web/API/Notification</see>
/// </summary>
public class Notification(IJSRuntime js) : IAsyncDisposable
{
    internal const string ClickMethodName = nameof(InvokeNotificationClick);
    internal const string ShowMethodName = nameof(InvokeNotificationShow);
    internal const string CloseMethodName = nameof(InvokeNotificationClose);
    internal const string ErrorMethodName = nameof(InvokeNotificationError);

    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Listener> _listeners = new();

    // Per-instance callback reference (see Keyboard): tracked notifications are isolated per circuit
    // / WASM app and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Notification>? _dotNetRef;
    private DotNetObjectReference<Notification> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>Removes a tracked notification's callbacks. Called by <see cref="NotificationHandle"/>.</summary>
    internal void RemoveListener(Guid id) => _listeners.TryRemove(id, out _);

    /// <summary>Invoked from JS on notification click. Dispatched via the per-instance ref.</summary>
    [JSInvokable(ClickMethodName)]
    public void InvokeNotificationClick(Guid id) { if (_listeners.TryGetValue(id, out var l)) l.OnClick?.Invoke(); }

    /// <summary>Invoked from JS when the notification is shown.</summary>
    [JSInvokable(ShowMethodName)]
    public void InvokeNotificationShow(Guid id) { if (_listeners.TryGetValue(id, out var l)) l.OnShow?.Invoke(); }

    /// <summary>Invoked from JS when the notification is closed. Also drops the listener so the
    /// map doesn't accumulate entries on natural dismiss or programmatic close.</summary>
    [JSInvokable(CloseMethodName)]
    public void InvokeNotificationClose(Guid id) { if (_listeners.TryRemove(id, out var l)) l.OnClose?.Invoke(); }

    /// <summary>Invoked from JS on a notification error.</summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeNotificationError(Guid id) { if (_listeners.TryGetValue(id, out var l)) l.OnError?.Invoke(); }

    /// <summary>
    /// Checks if the runtime (browser or web-view) is supporting the Web Notification API.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async ValueTask<bool> IsSupported()
    {
        return await js.Invoke<bool>("BitButil.notification.isSupported");
    }

    /// <summary>
    /// Gets the current permission of the Notification API.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Notification/permission_static">https://developer.mozilla.org/en-US/docs/Web/API/Notification/permission_static</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
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
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
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
    [DynamicDependency(nameof(InvokeNotificationClick), typeof(Notification))]
    [DynamicDependency(nameof(InvokeNotificationShow), typeof(Notification))]
    [DynamicDependency(nameof(InvokeNotificationClose), typeof(Notification))]
    [DynamicDependency(nameof(InvokeNotificationError), typeof(Notification))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NotificationOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InternalNotificationOptions))]
    public async ValueTask<NotificationHandle> ShowTracked(string title,
                                                           NotificationOptions? options = null,
                                                           Action? onClick = null,
                                                           Action? onShow = null,
                                                           Action? onClose = null,
                                                           Action? onError = null)
    {
        var id = Guid.NewGuid();
        _listeners.TryAdd(id, new Listener
        {
            OnClick = onClick,
            OnShow = onShow,
            OnClose = onClose,
            OnError = onError
        });

        InternalNotificationOptions? opts = options is null ? null : new(options);

        await js.InvokeVoid("BitButil.notification.showTracked", id, title, opts, DotNetRef);

        return new NotificationHandle(this, js, id);
    }

    public async ValueTask DisposeAsync()
    {
        // Detach any still-tracked notifications on the JS side before releasing the ref. Without
        // this, a notification left on screen would, on click/close, invoke a disposed
        // DotNetObjectReference and surface an error in the browser (see Window/History/Geolocation
        // which already clean up their JS-side listeners on disposal).
        try
        {
            if (_listeners.IsEmpty is false)
            {
                var ids = _listeners.Keys.ToArray();
                _listeners.Clear();
                await js.InvokeVoid("BitButil.notification.disposeAll", new object?[] { ids });
            }
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _listeners.Clear();
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }

        GC.SuppressFinalize(this);
    }

    private class Listener
    {
        public Action? OnClick { get; set; }
        public Action? OnShow { get; set; }
        public Action? OnClose { get; set; }
        public Action? OnError { get; set; }
    }
}
