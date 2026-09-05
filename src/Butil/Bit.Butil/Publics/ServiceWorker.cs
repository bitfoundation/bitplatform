using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/ServiceWorkerContainer">navigator.serviceWorker</see>.
/// </summary>
/// <remarks>
/// Service workers are origin-scoped and outlive the page, so this service intentionally
/// does not auto-unregister anything on disposal - the consuming app decides when to call
/// <see cref="Unregister"/>. Subscriptions returned by <see cref="SubscribeMessage"/> /
/// <see cref="SubscribeControllerChange"/> are detached on dispose.
/// </remarks>
// DotNetObjectReference.Create demands every public method of this type be preserved for trimming, and
// this type's public surface includes a [RequiresUnreferencedCode] JSON API (PostMessage<T>), so holding a
// DotNetObjectReference<ServiceWorker> field/property raises IL2026. The interop ref only ever dispatches
// the [JSInvokable] callbacks, never the JSON generic, and it keeps its own RUC/RDC attributes so a
// trimming/AOT consumer is still warned at the real call site. Scoped to this type (not assembly-wide).
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "DotNetObjectReference.Create preserves all public methods; the RUC JSON APIs it pulls in are never invoked through this ref and stay annotated for consumers.")]
[ButilService(typeof(ServiceWorker))]
public class ServiceWorker(IJSRuntime js) : IAsyncDisposable
{
    internal const string MessageMethodName = nameof(InvokeServiceWorkerMessage);
    internal const string ControllerChangeMethodName = nameof(InvokeServiceWorkerControllerChange);

    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Action<JsonElement>> _messageHandlers = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Action> _controllerChangeHandlers = new();

    // Per-instance callback reference (see Keyboard): subscriptions are isolated per circuit / WASM
    // app and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<ServiceWorker>? _dotNetRef;
    private DotNetObjectReference<ServiceWorker> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.serviceWorker</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.serviceWorker.isSupported");

    /// <summary>
    /// Invoked from JS on a worker message. Public + <see cref="JSInvokableAttribute"/> so it can be
    /// dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(MessageMethodName)]
    public void InvokeServiceWorkerMessage(Guid id, JsonElement data)
    {
        if (_messageHandlers.TryGetValue(id, out var handler)) handler.Invoke(data);
    }

    /// <summary>Invoked from JS when the controlling worker changes. See <see cref="InvokeServiceWorkerMessage"/>.</summary>
    [JSInvokable(ControllerChangeMethodName)]
    public void InvokeServiceWorkerControllerChange(Guid id)
    {
        if (_controllerChangeHandlers.TryGetValue(id, out var handler)) handler.Invoke();
    }

    /// <summary>
    /// Registers a service worker script. The promise resolves once the registration is created.
    /// </summary>
    /// <param name="scriptUrl">URL of the worker script (must be same-origin).</param>
    /// <param name="scope">Optional scope URL. When null, the script's directory is used.</param>
    /// <param name="updateViaCache">One of <c>"imports"</c>, <c>"all"</c>, <c>"none"</c>; null falls back to the browser default.</param>
    /// <param name="moduleType">When true, registers the worker as an ES module.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ServiceWorkerRegistrationInfo))]
    public ValueTask<ServiceWorkerRegistrationInfo> Register(string scriptUrl,
                                                             string? scope = null,
                                                             string? updateViaCache = null,
                                                             bool moduleType = false)
        => js.Invoke<ServiceWorkerRegistrationInfo>("BitButil.serviceWorker.register", scriptUrl, scope, updateViaCache, moduleType);

    /// <summary>
    /// Returns the registration matching <paramref name="scope"/> (or the most specific one for the
    /// document URL when null). <see cref="ServiceWorkerRegistrationInfo.IsRegistered"/> is false
    /// when no matching registration exists.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ServiceWorkerRegistrationInfo))]
    public ValueTask<ServiceWorkerRegistrationInfo> GetRegistration(string? scope = null)
        => js.Invoke<ServiceWorkerRegistrationInfo>("BitButil.serviceWorker.getRegistration", scope);

    /// <summary>
    /// Every registration this origin has, not just the one matching a scope. Useful for cleaning up
    /// workers left behind by an earlier version of an app.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ServiceWorkerRegistrationInfo))]
    public ValueTask<ServiceWorkerRegistrationInfo[]> GetRegistrations()
        => js.Invoke<ServiceWorkerRegistrationInfo[]>("BitButil.serviceWorker.getRegistrations");

    /// <summary>
    /// Waits until a service worker is <em>active</em> and returns its registration.
    /// </summary>
    /// <param name="timeoutMs">How long to wait before giving up. Defaults to 10 seconds.</param>
    /// <returns>
    /// The active registration, or an unregistered <see cref="ServiceWorkerRegistrationInfo"/> when
    /// the wait timed out or the browser has no service worker support.
    /// </returns>
    /// <remarks>
    /// <see cref="Register"/> returns as soon as the registration exists, which is usually while the
    /// worker is still installing - messages sent then go nowhere. This is the point at which
    /// <see cref="PostMessage"/> will actually reach it.
    /// <br/>
    /// The underlying <c>navigator.serviceWorker.ready</c> never rejects and never resolves when
    /// nothing is registered, so the timeout is what keeps this from hanging forever.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ServiceWorkerRegistrationInfo))]
    public ValueTask<ServiceWorkerRegistrationInfo> Ready(int timeoutMs = 10_000)
        => js.Invoke<ServiceWorkerRegistrationInfo>("BitButil.serviceWorker.ready", timeoutMs);

    /// <summary>Forces an update check for a registration.</summary>
    public ValueTask Update(string? scope = null) => js.InvokeVoid("BitButil.serviceWorker.update", scope);

    /// <summary>Unregisters the worker matching <paramref name="scope"/>. Returns true when something was removed.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Unregister(string? scope = null) => js.Invoke<bool>("BitButil.serviceWorker.unregister", scope);

    /// <summary>
    /// Sends <paramref name="message"/> to the active worker controlling this page.
    /// Returns false when no controller exists.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<bool> PostMessage<[DynamicallyAccessedMembers(JsonSerialized)] T>(T message)
        => js.Invoke<bool>("BitButil.serviceWorker.postMessage", message);

    /// <summary>
    /// Turns on <see href="https://developer.mozilla.org/en-US/docs/Web/API/NavigationPreloadManager">navigation preload</see>:
    /// the browser starts the navigation request in parallel with booting the worker, instead of
    /// after it.
    /// </summary>
    /// <param name="scope">Which registration, or null for the one matching the document URL.</param>
    /// <returns>False when the browser has no navigation preload, or the registration has no active worker yet.</returns>
    /// <remarks>
    /// This removes the tens to hundreds of milliseconds a cold worker start adds to the first
    /// navigation. It only pays off if the worker actually uses the preloaded response: the response
    /// arrives in its <c>fetch</c> handler as <c>event.preloadResponse</c>, and a worker that ignores
    /// it has made the browser issue the request twice.
    /// <br/>
    /// The state survives restarts - it belongs to the registration, not to the page - so this is
    /// something to call once after activation rather than on every load.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> EnableNavigationPreload(string? scope = null)
        => js.Invoke<bool>("BitButil.serviceWorker.enableNavigationPreload", scope);

    /// <summary>Turns navigation preload back off.</summary>
    /// <param name="scope">Which registration, or null for the one matching the document URL.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> DisableNavigationPreload(string? scope = null)
        => js.Invoke<bool>("BitButil.serviceWorker.disableNavigationPreload", scope);

    /// <summary>
    /// Sets the value of the <c>Service-Worker-Navigation-Preload</c> header the browser sends on
    /// preload requests, which is how the server can tell one apart from an ordinary request and
    /// answer it differently.
    /// </summary>
    /// <param name="value">The header value, e.g. a resource version or a fragment name.</param>
    /// <param name="scope">Which registration, or null for the one matching the document URL.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> SetNavigationPreloadHeader(string value, string? scope = null)
        => js.Invoke<bool>("BitButil.serviceWorker.setNavigationPreloadHeader", scope, value);

    /// <summary>Reads whether navigation preload is enabled, and with what header value.</summary>
    /// <param name="scope">Which registration, or null for the one matching the document URL.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NavigationPreloadState))]
    public ValueTask<NavigationPreloadState> GetNavigationPreloadState(string? scope = null)
        => js.Invoke<NavigationPreloadState>("BitButil.serviceWorker.navigationPreloadState", scope);

    /// <summary>
    /// Asks a waiting worker to call <c>skipWaiting()</c> - the "reload to update" button, without
    /// the reload.
    /// </summary>
    /// <param name="scope">Which registration, or null for the one matching the document URL.</param>
    /// <returns>False when no worker is waiting, which is the normal state.</returns>
    /// <remarks>
    /// A new worker installs and then waits until every page controlled by the old one is gone.
    /// <c>skipWaiting()</c> can only be called by the worker on itself, so this posts
    /// <c>{ __butil: 'skipWaiting' }</c> to it and the worker has to act on it:
    /// <code>
    /// self.addEventListener('message', event =&gt; {
    ///     if (event.data?.__butil === 'skipWaiting') self.skipWaiting();
    /// });
    /// </code>
    /// The activation that follows fires <see cref="SubscribeControllerChange"/>. Note that the new
    /// worker then takes over pages that loaded against the old one, so only do this where the app
    /// can handle its assets changing underneath it - or reload after the controller change.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> SkipWaiting(string? scope = null)
        => js.Invoke<bool>("BitButil.serviceWorker.skipWaiting", scope);

    /// <summary>
    /// Asks the active worker to call <c>clients.claim()</c>, taking control of pages that loaded
    /// before it activated - including the one calling this.
    /// </summary>
    /// <param name="scope">Which registration, or null for the one matching the document URL.</param>
    /// <param name="timeoutMs">How long to wait for the worker's answer before giving up.</param>
    /// <returns>False when there is no active worker, or it didn't answer.</returns>
    /// <remarks>
    /// A page that loaded without a controller keeps none for its whole life unless the worker
    /// claims it, which is why the first visit after installing a worker is the one where offline
    /// support quietly doesn't work.
    /// <br/>
    /// Like <see cref="SkipWaiting"/>, only the worker can do this, so it needs a handler that
    /// answers on the message's port:
    /// <code>
    /// self.addEventListener('message', event =&gt; {
    ///     if (event.data?.__butil !== 'claim') return;
    ///     event.waitUntil(self.clients.claim().then(() =&gt; event.ports[0]?.postMessage(true)));
    /// });
    /// </code>
    /// </remarks>
    public ValueTask<bool> Claim(string? scope = null, int timeoutMs = 5_000)
        => js.Invoke<bool>("BitButil.serviceWorker.claim", scope, timeoutMs);

    /// <summary>
    /// Lists the clients the worker controls - every tab, iframe and worker of this origin inside
    /// its scope.
    /// </summary>
    /// <param name="includeUncontrolled">Also report clients this worker doesn't control (other tabs of the origin outside its scope, or loaded before it activated).</param>
    /// <param name="type">Which kinds to report: <c>"window"</c>, <c>"worker"</c>, <c>"sharedworker"</c> or <c>"all"</c>.</param>
    /// <param name="scope">Which registration, or null for the one matching the document URL.</param>
    /// <param name="timeoutMs">How long to wait for the worker's answer before giving up.</param>
    /// <returns>
    /// The clients - which is empty when the worker reported none, and equally when there is no
    /// active worker or it didn't answer.
    /// </returns>
    /// <remarks>
    /// The <see href="https://developer.mozilla.org/en-US/docs/Web/API/Clients">Clients</see> API
    /// exists only on the worker's global scope, so this is a question asked over a
    /// <c>MessageChannel</c> and the worker has to answer it:
    /// <code>
    /// self.addEventListener('message', event =&gt; {
    ///     if (event.data?.__butil !== 'clients') return;
    ///     event.waitUntil(self.clients
    ///         .matchAll({ includeUncontrolled: event.data.includeUncontrolled, type: event.data.type })
    ///         .then(clients =&gt; event.ports[0]?.postMessage(clients.map(c =&gt; ({
    ///             id: c.id, url: c.url, type: c.type, frameType: c.frameType,
    ///             focused: c.focused, visibilityState: c.visibilityState
    ///         })))));
    /// });
    /// </code>
    /// An empty array is therefore ambiguous, and cannot be read as "the worker doesn't implement
    /// the protocol": the worker may equally have answered with no clients - the calling page is
    /// itself absent from the list while it is uncontrolled and <paramref name="includeUncontrolled"/>
    /// is false, as it is on the load that registered the worker - or there may have been no active
    /// worker to ask, or no answer within <paramref name="timeoutMs"/>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ServiceWorkerClientInfo))]
    public ValueTask<ServiceWorkerClientInfo[]> MatchAllClients(bool includeUncontrolled = false,
                                                                string type = "window",
                                                                string? scope = null,
                                                                int timeoutMs = 5_000)
        => js.Invoke<ServiceWorkerClientInfo[]>("BitButil.serviceWorker.matchAllClients", scope, includeUncontrolled, type, timeoutMs);

    /// <summary>
    /// Subscribes to messages broadcast from the service worker. The handler receives every
    /// payload as a <see cref="JsonElement"/>.
    /// </summary>
    [DynamicDependency(nameof(InvokeServiceWorkerMessage), typeof(ServiceWorker))]
    public async Task<ButilSubscription> SubscribeMessage(Action<JsonElement> handler)
    {
        var id = Guid.NewGuid();
        _messageHandlers.TryAdd(id, handler);
        await js.InvokeVoid("BitButil.serviceWorker.subscribeMessage", DotNetRef, id);
        return new ButilSubscription(id, async () =>
        {
            _messageHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.serviceWorker.unsubscribeMessage", id);
        });
    }

    /// <summary>Fires when <c>navigator.serviceWorker.controller</c> changes.</summary>
    [DynamicDependency(nameof(InvokeServiceWorkerControllerChange), typeof(ServiceWorker))]
    public async Task<ButilSubscription> SubscribeControllerChange(Action handler)
    {
        var id = Guid.NewGuid();
        _controllerChangeHandlers.TryAdd(id, handler);
        await js.InvokeVoid("BitButil.serviceWorker.subscribeControllerChange", DotNetRef, id);
        return new ButilSubscription(id, async () =>
        {
            _controllerChangeHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.serviceWorker.unsubscribeControllerChange", id);
        });
    }

    /// <summary>Unsubscribes every message and controller-change handler this instance registered, and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            var messageIds = System.Linq.Enumerable.ToArray(_messageHandlers.Keys);
            var controllerIds = System.Linq.Enumerable.ToArray(_controllerChangeHandlers.Keys);
            _messageHandlers.Clear();
            _controllerChangeHandlers.Clear();
            foreach (var id in messageIds)
                await js.InvokeVoid("BitButil.serviceWorker.unsubscribeMessage", id);
            foreach (var id in controllerIds)
                await js.InvokeVoid("BitButil.serviceWorker.unsubscribeControllerChange", id);
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}
