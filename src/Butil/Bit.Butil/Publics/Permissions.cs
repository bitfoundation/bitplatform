using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/Permissions">navigator.permissions</see>.
/// </summary>
[ButilService(typeof(Permissions))]
public class Permissions(IJSRuntime js) : IAsyncDisposable
{
    internal const string ChangeMethodName = nameof(InvokePermissionChange);

    private readonly ConcurrentDictionary<Guid, Action<PermissionState>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Permissions>? _dotNetRef;
    private DotNetObjectReference<Permissions> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.permissions</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported()
        => js.Invoke<bool>("BitButil.permissions.isSupported");

    /// <summary>
    /// Returns the current state for a given permission descriptor name.
    /// </summary>
    /// <param name="name">A descriptor name such as <c>"geolocation"</c>, <c>"notifications"</c>,
    /// <c>"camera"</c>, <c>"microphone"</c>, <c>"clipboard-read"</c>, <c>"clipboard-write"</c>,
    /// <c>"push"</c>, etc. Browser support varies; unknown names return <see cref="PermissionState.Unknown"/>.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<PermissionState> Query(string name)
        => ToState(await js.Invoke<string>("BitButil.permissions.query", name));

    /// <summary>Invoked from JS when a watched permission's state changes. See <see cref="SubscribeChange"/>.</summary>
    [JSInvokable(ChangeMethodName)]
    public void InvokePermissionChange(Guid id, string state)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(ToState(state));
    }

    /// <summary>
    /// Watches a permission and calls <paramref name="handler"/> whenever its state changes.
    /// </summary>
    /// <param name="name">The same descriptor name <see cref="Query"/> takes.</param>
    /// <param name="handler">Called with the new state on each change.</param>
    /// <returns>
    /// The state at subscription time, plus a subscription to dispose. The subscription is null
    /// when the browser has no Permissions API or doesn't recognize <paramref name="name"/> - there
    /// is nothing to watch in that case, and the state will be <see cref="PermissionState.Unknown"/>.
    /// </returns>
    /// <remarks>
    /// This is the only way to notice that a user revoked a grant from browser UI rather than from
    /// the page - polling <see cref="Query"/> would be the alternative. The initial state is
    /// returned alongside the subscription so callers don't have to call <see cref="Query"/> first
    /// and race the handler.
    /// </remarks>
    [DynamicDependency(nameof(InvokePermissionChange), typeof(Permissions))]
    public async Task<(PermissionState State, ButilSubscription? Subscription)> SubscribeChange(string name, Action<PermissionState> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers[id] = handler;

        var state = ToState(await js.Invoke<string>("BitButil.permissions.subscribe", DotNetRef, id, name));
        if (state == PermissionState.Unknown)
        {
            _handlers.TryRemove(id, out _);
            return (state, null);
        }

        return (state, new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.permissions.unsubscribe", id);
        }));
    }

    // Internal rather than private: every service that reads a permission - Sensors,
    // WindowManagement, IdleDetector - has to answer in PermissionState, and one mapping is what
    // keeps an unrecognized state meaning the same thing on all of them.
    internal static PermissionState ToState(string? raw) => raw switch
    {
        "granted" => PermissionState.Granted,
        "denied" => PermissionState.Denied,
        "prompt" => PermissionState.Prompt,
        _ => PermissionState.Unknown,
    };

    /// <summary>
    /// On scope/circuit teardown, detaches any listener whose <see cref="ButilSubscription"/> was
    /// never disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.permissions.disposeAll");
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
