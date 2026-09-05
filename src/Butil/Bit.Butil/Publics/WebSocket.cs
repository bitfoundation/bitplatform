using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the browser's <see href="https://developer.mozilla.org/en-US/docs/Web/API/WebSocket">WebSocket</see>:
/// a two-way, message-oriented connection that stays open.
/// </summary>
/// <remarks>
/// <see cref="EventSource"/> is the one-way half of this - the server sends, the browser reconnects
/// by itself. A WebSocket sends in both directions and never reconnects on its own, so a client that
/// wants to come back has to say so.
/// <br/>
/// Under Blazor WebAssembly <c>ClientWebSocket</c> is already emulated over this very API, so the
/// reason to come here rather than use it is what the emulation hides: binary frames without a copy
/// through a managed stream, the close code and reason, the negotiated sub-protocol, and
/// <see cref="WebSocketHandle.GetBufferedAmount"/> - the only back-pressure signal a browser socket
/// offers.
/// </remarks>
[ButilService(typeof(WebSocket))]
public class WebSocket(IJSRuntime js) : IAsyncDisposable
{
    internal const string OpenMethodName = nameof(InvokeWebSocketOpen);
    internal const string MessageMethodName = nameof(InvokeWebSocketMessage);
    internal const string CloseMethodName = nameof(InvokeWebSocketClose);
    internal const string ErrorMethodName = nameof(InvokeWebSocketError);

    private readonly ConcurrentDictionary<Guid, WebSocketHandlers> _handlers = new();

    // Per-instance callback reference (see Keyboard): sockets are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<WebSocket>? _dotNetRef;
    private DotNetObjectReference<WebSocket> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    private sealed record WebSocketHandlers(
        Action<WebSocketMessage> OnMessage,
        Action<string, string>? OnOpen,
        Action<WebSocketClose>? OnClose,
        Action? OnError);

    /// <summary>True when the runtime exposes <c>WebSocket</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (<c>false</c>) rather than
    /// throwing, so the result can't be distinguished from a genuine value. If you branch on it,
    /// defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.webSocket.isSupported");

    /// <summary>
    /// Invoked from JS when the socket opens. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(OpenMethodName)]
    public void InvokeWebSocketOpen(Guid id, string protocol, string extensions)
    {
        if (_handlers.TryGetValue(id, out var handlers)) handlers.OnOpen?.Invoke(protocol, extensions);
    }

    /// <summary>
    /// Invoked from JS for each frame. Public + <see cref="JSInvokableAttribute"/> so it can be
    /// dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(MessageMethodName)]
    public void InvokeWebSocketMessage(Guid id, bool isBinary, string? text, byte[]? data)
    {
        if (_handlers.TryGetValue(id, out var handlers))
            handlers.OnMessage.Invoke(new WebSocketMessage(isBinary, text, data));
    }

    /// <summary>
    /// Invoked from JS when the socket closes. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(CloseMethodName)]
    public void InvokeWebSocketClose(Guid id, int code, string reason, bool wasClean)
    {
        // The socket is gone: drop the handlers with it rather than waiting for a Dispose that a
        // caller who only ever listened has no reason to make.
        if (_handlers.TryRemove(id, out var handlers))
            handlers.OnClose?.Invoke(new WebSocketClose(code, reason, wasClean));
    }

    /// <summary>
    /// Invoked from JS when the socket errors. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeWebSocketError(Guid id)
    {
        if (_handlers.TryGetValue(id, out var handlers)) handlers.OnError?.Invoke();
    }

    /// <summary>
    /// Opens a connection.
    /// </summary>
    /// <param name="url">
    /// A <c>ws://</c> or <c>wss://</c> URL. An <c>http://</c> or <c>https://</c> one is accepted too
    /// and rewritten to the matching socket scheme; any other scheme is refused outright. A page
    /// served over HTTPS may only open <c>wss://</c> - a <c>ws://</c> URL is blocked as mixed
    /// content before the connection is attempted.
    /// </param>
    /// <param name="onMessage">
    /// Called for every frame. <see cref="WebSocketMessage.IsBinary"/> says which of
    /// <see cref="WebSocketMessage.Text"/> and <see cref="WebSocketMessage.Data"/> carries it.
    /// </param>
    /// <param name="protocols">
    /// Sub-protocols to offer, most-preferred first. The server picks one, which arrives as the
    /// first argument of <paramref name="onOpen"/>; offering a protocol the server does not know
    /// fails the connection rather than falling back to none.
    /// </param>
    /// <param name="onOpen">Called once the connection is established, with the negotiated protocol and extensions.</param>
    /// <param name="onClose">
    /// Called when the connection ends, cleanly or not. Code 1006 with
    /// <see cref="WebSocketClose.WasClean"/> false is the browser's stand-in for "the connection
    /// dropped" - no close frame ever arrived, so there is nothing more specific to report.
    /// </param>
    /// <param name="onError">
    /// Called on a connection failure. It carries nothing by design - the specification keeps the
    /// detail back so a page cannot probe cross-origin ports with it. The
    /// <paramref name="onClose"/> that follows is where the code and reason are.
    /// </param>
    /// <returns>A handle, or null when the runtime has no <c>WebSocket</c>, the URL is malformed, or mixed content blocked it.</returns>
    /// <remarks>
    /// Nothing here reconnects. A WebSocket that drops stays dropped until something calls
    /// <see cref="Open"/> again - unlike <see cref="EventSource"/>, where the browser retries by
    /// itself. Dispose the handle when you're done.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebSocketMessage))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebSocketClose))]
    public async ValueTask<WebSocketHandle?> Open(string url,
                                                  Action<WebSocketMessage> onMessage,
                                                  string[]? protocols = null,
                                                  Action<string, string>? onOpen = null,
                                                  Action<WebSocketClose>? onClose = null,
                                                  Action? onError = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(onMessage);

        var id = Guid.NewGuid();
        _handlers[id] = new WebSocketHandlers(onMessage, onOpen, onClose, onError);

        var opened = await js.Invoke<bool>("BitButil.webSocket.open", DotNetRef, id, url, protocols ?? []);

        if (opened is false)
        {
            _handlers.TryRemove(id, out _);
            return null;
        }

        return new WebSocketHandle(js, id, () => _handlers.TryRemove(id, out _));
    }

    /// <summary>
    /// On scope/circuit teardown, closes any socket whose <see cref="WebSocketHandle"/> was never
    /// disposed, so an abandoned connection can't outlive the page that opened it.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.webSocket.disposeAll");
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
