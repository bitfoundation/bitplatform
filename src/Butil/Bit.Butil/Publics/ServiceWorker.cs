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
/// does not auto-unregister anything on disposal — the consuming app decides when to call
/// <see cref="Unregister"/>. Subscriptions returned by <see cref="SubscribeMessage"/> /
/// <see cref="SubscribeControllerChange"/> are detached on dispose.
/// </remarks>
public class ServiceWorker(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>navigator.serviceWorker</c>.</summary>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.serviceWorker.isSupported");

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
    public ValueTask<bool> Unregister(string? scope = null) => js.Invoke<bool>("BitButil.serviceWorker.unregister", scope);

    /// <summary>
    /// Sends <paramref name="message"/> to the active worker controlling this page.
    /// Returns false when no controller exists.
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<bool> PostMessage<[DynamicallyAccessedMembers(JsonSerialized)] T>(T message)
        => js.Invoke<bool>("BitButil.serviceWorker.postMessage", message);

    /// <summary>
    /// Subscribes to messages broadcast from the service worker. The handler receives every
    /// payload as a <see cref="JsonElement"/>.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ServiceWorkerListenersManager))]
    public async Task<ButilSubscription> SubscribeMessage(Action<JsonElement> handler)
    {
        var id = ServiceWorkerListenersManager.AddMessageListener(handler);
        await js.InvokeVoid("BitButil.serviceWorker.subscribeMessage",
            ServiceWorkerListenersManager.MessageMethodName, id);
        return new ButilSubscription(id, async () =>
        {
            ServiceWorkerListenersManager.RemoveMessageListener(id);
            if (OperatingSystem.IsBrowser() is false) return;
            await js.InvokeVoid("BitButil.serviceWorker.unsubscribeMessage", id);
        });
    }

    /// <summary>Fires when <c>navigator.serviceWorker.controller</c> changes.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ServiceWorkerListenersManager))]
    public async Task<ButilSubscription> SubscribeControllerChange(Action handler)
    {
        var id = ServiceWorkerListenersManager.AddControllerChangeListener(handler);
        await js.InvokeVoid("BitButil.serviceWorker.subscribeControllerChange",
            ServiceWorkerListenersManager.ControllerChangeMethodName, id);
        return new ButilSubscription(id, async () =>
        {
            ServiceWorkerListenersManager.RemoveControllerChangeListener(id);
            if (OperatingSystem.IsBrowser() is false) return;
            await js.InvokeVoid("BitButil.serviceWorker.unsubscribeControllerChange", id);
        });
    }
}
