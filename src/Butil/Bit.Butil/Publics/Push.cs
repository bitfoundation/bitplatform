using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Push_API">Push API</see>'s
/// subscription surface.
/// </summary>
/// <remarks>
/// Push requires an active service worker registration. Register one via <see cref="ServiceWorker"/>
/// before subscribing.
/// </remarks>
public class Push(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>ServiceWorkerRegistration.pushManager</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.push.isSupported");

    /// <summary>
    /// Returns the existing subscription for the active service worker, or an inactive payload
    /// when none exists.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PushSubscriptionInfo))]
    public ValueTask<PushSubscriptionInfo> GetSubscription()
        => js.Invoke<PushSubscriptionInfo>("BitButil.push.getSubscription");

    /// <summary>
    /// Subscribes the current service worker to push messages.
    /// </summary>
    /// <param name="applicationServerKey">VAPID public key, base64-url encoded.</param>
    /// <param name="userVisibleOnly">Required to be true on Chromium; the browser must be able to
    /// show a user-visible notification for each push.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PushSubscriptionInfo))]
    public ValueTask<PushSubscriptionInfo> Subscribe(string applicationServerKey, bool userVisibleOnly = true)
        => js.Invoke<PushSubscriptionInfo>("BitButil.push.subscribe", applicationServerKey, userVisibleOnly);

    /// <summary>Unsubscribes the active subscription. Returns true if a subscription was removed.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Unsubscribe() => js.Invoke<bool>("BitButil.push.unsubscribe");
}
