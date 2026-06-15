using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/BroadcastChannel">BroadcastChannel</see>
/// API for cross-tab pub/sub on the same origin.
/// </summary>
/// <remarks>
/// Each <see cref="BroadcastChannel"/> instance can host any number of named channels - a
/// new JS-side channel object is created on first <see cref="Subscribe"/> per name and torn
/// down only when every subscription on that name has been disposed.
/// </remarks>
// DotNetObjectReference.Create demands every public method of this type be preserved for trimming, and
// this type's public surface includes a [RequiresUnreferencedCode] JSON API (Post<T>), so holding a
// DotNetObjectReference<BroadcastChannel> field/property raises IL2026. The interop ref only ever
// dispatches the [JSInvokable] callbacks, never the JSON generic, and it keeps its own RUC/RDC attributes
// so a trimming/AOT consumer is still warned at the real call site. Scoped to this type (not assembly-wide).
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "DotNetObjectReference.Create preserves all public methods; the RUC JSON APIs it pulls in are never invoked through this ref and stay annotated for consumers.")]
public class BroadcastChannel(IJSRuntime js) : IAsyncDisposable
{
    internal const string MessageMethodName = nameof(InvokeBroadcastChannelMessage);
    internal const string ErrorMethodName = nameof(InvokeBroadcastChannelError);

    private readonly ConcurrentDictionary<Guid, Listener> _subscriptions = new();

    // Per-instance callback reference (see Keyboard): subscriptions are isolated per circuit / WASM
    // app and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<BroadcastChannel>? _dotNetRef;
    private DotNetObjectReference<BroadcastChannel> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>BroadcastChannel</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.broadcastChannel.isSupported");

    /// <summary>
    /// Invoked from JS for each channel message. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(MessageMethodName)]
    public void InvokeBroadcastChannelMessage(Guid id, JsonElement data)
    {
        if (_subscriptions.TryGetValue(id, out var listener)) listener.OnMessage?.Invoke(data);
    }

    /// <summary>Invoked from JS on a channel <c>messageerror</c>. See <see cref="InvokeBroadcastChannelMessage"/>.</summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeBroadcastChannelError(Guid id)
    {
        if (_subscriptions.TryGetValue(id, out var listener)) listener.OnError?.Invoke();
    }

    /// <summary>
    /// Sends <paramref name="message"/> to every other listener on <paramref name="channelName"/>
    /// in the same origin (the sender does not receive its own message - that's the spec).
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask Post<[DynamicallyAccessedMembers(JsonSerialized)] T>(string channelName, T message)
        => js.InvokeVoid("BitButil.broadcastChannel.post", channelName, message);

    /// <summary>
    /// Subscribes to <paramref name="channelName"/>. The handler receives every message as a
    /// <see cref="JsonElement"/> so callers can deserialize into whatever shape they expect.
    /// Use the returned <see cref="ButilSubscription"/> to detach.
    /// </summary>
    [DynamicDependency(nameof(InvokeBroadcastChannelMessage), typeof(BroadcastChannel))]
    [DynamicDependency(nameof(InvokeBroadcastChannelError), typeof(BroadcastChannel))]
    public async Task<ButilSubscription> Subscribe(string channelName,
        Action<JsonElement>? onMessage,
        Action? onError = null)
    {
        if (onMessage is null && onError is null)
            throw new ArgumentException("At least one of onMessage or onError must be provided.");

        var id = Guid.NewGuid();
        _subscriptions.TryAdd(id, new Listener { OnMessage = onMessage, OnError = onError });

        await js.InvokeVoid("BitButil.broadcastChannel.subscribe", DotNetRef, id, channelName);

        return new ButilSubscription(id, async () =>
        {
            _subscriptions.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.broadcastChannel.unsubscribe", id);
        });
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_subscriptions.IsEmpty is false)
            {
                var ids = _subscriptions.Keys.ToArray();
                _subscriptions.Clear();
                foreach (var id in ids)
                {
                    await js.InvokeVoid("BitButil.broadcastChannel.unsubscribe", id);
                }
            }
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }

    private class Listener
    {
        public Action<JsonElement>? OnMessage { get; set; }
        public Action? OnError { get; set; }
    }
}
