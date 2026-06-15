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
