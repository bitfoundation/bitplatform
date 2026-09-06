using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/MessageChannel">MessageChannel</see>
/// and <see href="https://developer.mozilla.org/en-US/docs/Web/API/MessagePort">MessagePort</see>:
/// a two-ended pipe you can hand one end of to somewhere else - a worker, an iframe, a service
/// worker - and then talk over privately.
/// </summary>
/// <remarks>
/// A port is the browser's unit of private conversation. Where <see cref="BroadcastChannel"/> shouts
/// to everyone on the origin, a channel is between exactly two holders, and a port belongs to one
/// context at a time: giving a port away <em>transfers</em> it, and this side stops being able to
/// use it.
/// <br/>
/// Nothing is delivered until a port is started (<see cref="MessagePortHandle.Start"/>). Messages
/// sent before that are queued rather than lost, which is what makes "create it, hand it over,
/// listen, then open it" work.
/// </remarks>
[ButilService(typeof(MessageChannel))]
public class MessageChannel(IJSRuntime js) : IAsyncDisposable
{
    internal const string MessageMethodName = nameof(InvokePortMessage);

    // port id -> its .NET listeners. One JS listener per port fans out to all of them, so a second
    // subscription doesn't re-attach anything in the browser.
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Action<ButilMessage>>> _handlers = new();

    // Per-instance callback reference (see Keyboard): ports are isolated per circuit / WASM app and
    // released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<MessageChannel>? _dotNetRef;
    internal DotNetObjectReference<MessageChannel> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>MessageChannel</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (<c>false</c>) rather than
    /// throwing, so the result can't be distinguished from a genuine value. If you branch on it,
    /// defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.messageChannel.isSupported");

    /// <summary>
    /// Invoked from JS for each message on a port. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(MessageMethodName)]
    public void InvokePortMessage(Guid portId, bool isBinary, string? json, byte[]? data)
    {
        if (_handlers.TryGetValue(portId, out var listeners) is false) return;

        var message = new ButilMessage(isBinary, json, data);
        foreach (var listener in listeners.Values) listener(message);
    }

    /// <summary>
    /// Creates a channel and its two ports.
    /// </summary>
    /// <returns>A handle, or null when the runtime has no <c>MessageChannel</c>.</returns>
    /// <remarks>
    /// Keep one port and give the other away. Neither delivers anything until it is started.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilMessage))]
    public async ValueTask<MessageChannelHandle?> Create()
    {
        var channelId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        var created = await js.Invoke<bool>("BitButil.messageChannel.create", channelId, firstId, secondId);

        return created ? new MessageChannelHandle(js, this, channelId, firstId, secondId) : null;
    }

    /// <summary>
    /// Registers a .NET listener for a port, attaching the JS listener the first time.
    /// </summary>
    internal async ValueTask<ButilSubscription?> AddPortListener(Guid portId, Action<ButilMessage> onMessage)
    {
        ArgumentNullException.ThrowIfNull(onMessage);

        var listeners = _handlers.GetOrAdd(portId, static _ => new ConcurrentDictionary<Guid, Action<ButilMessage>>());
        var listenerId = Guid.NewGuid();
        listeners[listenerId] = onMessage;

        // Idempotent on the JS side: it replaces the one listener that fans out to all of these.
        var attached = await js.Invoke<bool>("BitButil.messageChannel.listen", DotNetRef, portId);

        if (attached is false)
        {
            listeners.TryRemove(listenerId, out _);
            return null;
        }

        return new ButilSubscription(listenerId, () =>
        {
            listeners.TryRemove(listenerId, out _);
            return ValueTask.CompletedTask;
        });
    }

    /// <summary>Forgets every .NET listener for a port. Called when the port itself is released.</summary>
    internal void RemovePortListeners(Guid portId) => _handlers.TryRemove(portId, out _);

    /// <summary>
    /// On scope/circuit teardown, closes and releases every port and channel whose handle was never
    /// disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.messageChannel.disposeAll");
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
