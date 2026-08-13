using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events">server-sent events</see>
/// (<c>EventSource</c>): a one-way stream of text events pushed from the server over an ordinary
/// HTTP connection, with automatic reconnection built in.
/// </summary>
/// <remarks>
/// The simpler half of a WebSocket. The server only ever sends and the client only ever listens,
/// which suits live dashboards, progress feeds and notifications - and unlike a WebSocket, it
/// reconnects itself and resumes from the last event id without any code on your side.
/// <br/>
/// Blazor Server has SignalR for this. Where this earns its place is Blazor WebAssembly talking to
/// an endpoint that speaks <c>text/event-stream</c>, and anywhere you want the browser's own
/// reconnect behaviour rather than your own.
/// </remarks>
public class EventSource(IJSRuntime js) : IAsyncDisposable
{
    internal const string OpenMethodName = nameof(InvokeEventSourceOpen);
    internal const string MessageMethodName = nameof(InvokeEventSourceMessage);
    internal const string ErrorMethodName = nameof(InvokeEventSourceError);

    private readonly ConcurrentDictionary<Guid, EventSourceHandlers> _handlers = new();

    // Per-instance callback reference (see Keyboard): streams are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<EventSource>? _dotNetRef;
    private DotNetObjectReference<EventSource> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    private sealed record EventSourceHandlers(
        Action<ServerSentEvent> OnMessage,
        Action? OnOpen,
        Action<bool>? OnError);

    /// <summary>True when the runtime exposes <c>EventSource</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.eventSource.isSupported");

    /// <summary>
    /// Invoked from JS when the stream connects. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(OpenMethodName)]
    public void InvokeEventSourceOpen(Guid id)
    {
        if (_handlers.TryGetValue(id, out var handlers)) handlers.OnOpen?.Invoke();
    }

    /// <summary>
    /// Invoked from JS for each event. Public + <see cref="JSInvokableAttribute"/> so it can be
    /// dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(MessageMethodName)]
    public void InvokeEventSourceMessage(Guid id, string eventName, string data, string lastEventId)
    {
        if (_handlers.TryGetValue(id, out var handlers))
            handlers.OnMessage.Invoke(new ServerSentEvent(eventName, data, lastEventId));
    }

    /// <summary>
    /// Invoked from JS when the stream errors. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeEventSourceError(Guid id, bool fatal)
    {
        if (_handlers.TryGetValue(id, out var handlers)) handlers.OnError?.Invoke(fatal);
    }

    /// <summary>
    /// Opens a stream and starts listening.
    /// </summary>
    /// <param name="url">An endpoint serving <c>text/event-stream</c>. Same-origin unless the server sends CORS headers.</param>
    /// <param name="onMessage">
    /// Called for every event. Unnamed events arrive with an <see cref="ServerSentEvent.EventName"/>
    /// of <c>"message"</c>; named ones carry the server's own name.
    /// </param>
    /// <param name="eventNames">
    /// The named events to listen for. Named events never reach the default <c>message</c>
    /// listener, so anything the server names has to be declared here to be seen at all.
    /// </param>
    /// <param name="onOpen">Called when the connection is established - including after a reconnect.</param>
    /// <param name="onError">
    /// Called on a connection failure. The argument is true when the stream is finished for good
    /// and false when the browser is going to retry on its own, which it does by default.
    /// </param>
    /// <param name="withCredentials">When true, sends cookies and HTTP auth cross-origin. Requires the server to allow credentials.</param>
    /// <returns>A handle, or null when the runtime has no <c>EventSource</c> or the URL is malformed.</returns>
    /// <remarks>
    /// Dispose the handle when you're done. An <c>EventSource</c> that is not closed keeps
    /// reconnecting on the browser's own schedule for as long as the page lives.
    /// </remarks>
    public async ValueTask<EventSourceHandle?> Open(string url,
                                                     Action<ServerSentEvent> onMessage,
                                                     string[]? eventNames = null,
                                                     Action? onOpen = null,
                                                     Action<bool>? onError = null,
                                                     bool withCredentials = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(onMessage);

        var id = Guid.NewGuid();
        _handlers[id] = new EventSourceHandlers(onMessage, onOpen, onError);

        var opened = await js.Invoke<bool>("BitButil.eventSource.open",
            DotNetRef, id, url, withCredentials, eventNames ?? []);

        if (opened is false)
        {
            _handlers.TryRemove(id, out _);
            return null;
        }

        return new EventSourceHandle(js, id, () => _handlers.TryRemove(id, out _));
    }

    /// <summary>
    /// On scope/circuit teardown, closes any stream whose <see cref="EventSourceHandle"/> was never
    /// disposed, so an abandoned connection can't keep reconnecting.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.eventSource.disposeAll");
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
