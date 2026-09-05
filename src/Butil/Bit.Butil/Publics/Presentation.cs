using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Presentation_API">Presentation API</see>:
/// opens one of your own pages on a second display - a projector, a TV, a cast receiver - and keeps a
/// message channel to it.
/// </summary>
/// <remarks>
/// Where <see cref="RemotePlayback"/> hands a single media element to a device, this hands over a
/// whole page: the receiver loads a URL you nominate, and the two halves talk over a
/// <see cref="PresentationConnectionHandle"/>. That makes it the API behind slide decks, dashboards
/// and anything where the second screen shows something other than a copy of the first.
/// <br/>
/// Both halves live here. A controlling page calls <see cref="Start"/> (from a user gesture) or
/// <see cref="Reconnect"/>; the page that ends up on the second screen calls
/// <see cref="WatchReceiverConnections"/> to find the controllers that opened it.
/// <br/>
/// A presentation outlives the connection to it: closing merely lets go, so a controller that
/// navigates away can come back with the id it kept. Ending it is
/// <see cref="PresentationConnectionHandle.Terminate"/>.
/// </remarks>
[ButilService(typeof(Presentation))]
public class Presentation(IJSRuntime js) : IAsyncDisposable
{
    internal const string MessageMethodName = nameof(InvokePresentationMessage);
    internal const string StateMethodName = nameof(InvokePresentationState);
    internal const string ConnectionMethodName = nameof(InvokePresentationConnection);
    internal const string AvailabilityMethodName = nameof(InvokePresentationAvailability);

    private readonly ConcurrentDictionary<Guid, Action<PresentationMessage>> _messageHandlers = new();
    private readonly ConcurrentDictionary<Guid, Action<PresentationConnectionState>> _stateHandlers = new();
    private readonly ConcurrentDictionary<Guid, Action<bool>> _availabilityHandlers = new();

    private Action<PresentationConnectionHandle>? _onReceiverConnection;
    private Action<PresentationMessage>? _receiverMessageHandler;
    private Action<PresentationConnectionState>? _receiverStateHandler;

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Presentation>? _dotNetRef;
    private DotNetObjectReference<Presentation> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>PresentationRequest</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.presentation.isSupported");

    /// <summary>
    /// True when this page <em>is</em> the presentation - it was opened on the second screen by a
    /// controlling page, so <c>navigator.presentation.receiver</c> exists.
    /// </summary>
    /// <remarks>
    /// The branch a page that serves as both controller and receiver takes at startup.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsReceiver() => js.Invoke<bool>("BitButil.presentation.isReceiver");

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PresentationRequest/start">PresentationRequest.start()</see>:
    /// opens the browser's device picker and, once the user chooses, loads the page there.
    /// </summary>
    /// <param name="urls">
    /// The URLs the receiver may load, in preference order - more than one lets different device
    /// types get different pages.
    /// </param>
    /// <param name="onMessage">Called with each message the receiving page sends back.</param>
    /// <param name="onStateChange">Called when the connection connects, closes or terminates.</param>
    /// <returns>A connection handle, or <c>null</c> when the user dismissed the picker or no device accepted the URL.</returns>
    /// <remarks>
    /// Must be called from a user-gesture handler such as a click. Keep
    /// <see cref="PresentationConnectionHandle.ConnectionId"/> if the app should be able to rejoin
    /// this presentation later.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PresentationConnectionJsInfo))]
    public async ValueTask<PresentationConnectionHandle?> Start(string[] urls,
                                                               Action<PresentationMessage>? onMessage = null,
                                                               Action<PresentationConnectionState>? onStateChange = null)
    {
        var id = Guid.NewGuid();
        Register(id, onMessage, onStateChange);

        PresentationConnectionJsInfo? info;
        try
        {
            info = await js.Invoke<PresentationConnectionJsInfo?>("BitButil.presentation.start",
                                                                  id, urls, DotNetRef, MessageMethodName, StateMethodName);
        }
        catch
        {
            // The handlers are registered before the call, since JS can dispatch to them the moment
            // it has them; a throw leaves nobody holding a handle that could ever unregister them.
            Unregister(id);
            throw;
        }

        return ToHandle(id, info);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PresentationRequest/reconnect">PresentationRequest.reconnect()</see>:
    /// rejoins a presentation that is still running on the second screen.
    /// </summary>
    /// <param name="urls">The same URLs the presentation was started with.</param>
    /// <param name="presentationId">The id kept from <see cref="PresentationConnectionHandle.ConnectionId"/>.</param>
    /// <param name="onMessage">Called with each message the receiving page sends back.</param>
    /// <param name="onStateChange">Called when the connection connects, closes or terminates.</param>
    /// <returns>A connection handle, or <c>null</c> when no presentation with that id is still running.</returns>
    /// <remarks>
    /// Needs no user gesture and no picker - which is what makes it usable on page load, to restore
    /// control of a screen the user left running.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PresentationConnectionJsInfo))]
    public async ValueTask<PresentationConnectionHandle?> Reconnect(string[] urls,
                                                                    string presentationId,
                                                                    Action<PresentationMessage>? onMessage = null,
                                                                    Action<PresentationConnectionState>? onStateChange = null)
    {
        var id = Guid.NewGuid();
        Register(id, onMessage, onStateChange);

        PresentationConnectionJsInfo? info;
        try
        {
            info = await js.Invoke<PresentationConnectionJsInfo?>("BitButil.presentation.reconnect",
                                                                  id, urls, presentationId, DotNetRef, MessageMethodName, StateMethodName);
        }
        catch
        {
            Unregister(id);   // same as Start: nothing else would ever release the handlers
            throw;
        }

        return ToHandle(id, info);
    }

    /// <summary>
    /// Sets <see href="https://developer.mozilla.org/en-US/docs/Web/API/Presentation/defaultRequest">navigator.presentation.defaultRequest</see>,
    /// the request the browser's own "cast this page" menu item starts.
    /// </summary>
    /// <param name="urls">The URLs to present, or an empty array to clear the default request.</param>
    /// <returns>False when the API is unavailable.</returns>
    /// <remarks>
    /// Without this, the browser's built-in casting entry point does nothing for the page - it is the
    /// only way to be reachable from UI the app doesn't draw. The connection it produces arrives
    /// through <see cref="WatchReceiverConnections"/> on the receiving side.
    /// </remarks>
    public ValueTask<bool> SetDefaultRequest(string[] urls)
        => js.Invoke<bool>("BitButil.presentation.setDefaultRequest", (object)(urls ?? []));

    /// <summary>
    /// Invoked from JS when a presentation connection delivers a message. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(MessageMethodName)]
    public void InvokePresentationMessage(Guid id, string? text, byte[]? data)
    {
        if (_messageHandlers.TryGetValue(id, out var handler)) handler.Invoke(new PresentationMessage(text, data));
    }

    /// <summary>
    /// Invoked from JS when a presentation connection changes state. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(StateMethodName)]
    public void InvokePresentationState(Guid id, string state)
    {
        if (_stateHandlers.TryGetValue(id, out var handler)) handler.Invoke(ToConnectionState(state));
    }

    /// <summary>
    /// Invoked from JS when a controlling page connects to this receiving page. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ConnectionMethodName)]
    public void InvokePresentationConnection(Guid id, string connectionId, string url, string state)
    {
        var onConnection = _onReceiverConnection;
        if (onConnection is null) return;

        Register(id, _receiverMessageHandler, _receiverStateHandler);
        onConnection.Invoke(new PresentationConnectionHandle(js, id, connectionId, url, () => Unregister(id)));
    }

    /// <summary>
    /// Invoked from JS when the availability of presentation displays changes. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(AvailabilityMethodName)]
    public void InvokePresentationAvailability(Guid id, bool available)
    {
        if (_availabilityHandlers.TryGetValue(id, out var handler)) handler.Invoke(available);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PresentationRequest/getAvailability">PresentationRequest.getAvailability()</see>:
    /// whether there is any display these URLs could be presented on.
    /// </summary>
    /// <param name="urls">The URLs the presentation would use.</param>
    /// <param name="handler">Called with the current answer straight away, and again whenever it changes.</param>
    /// <returns>
    /// A subscription, or <c>null</c> when the engine cannot monitor availability continuously - in
    /// which case the documented fallback is to offer the button anyway and let
    /// <see cref="Start"/> report the truth.
    /// </returns>
    /// <remarks>
    /// Monitoring costs battery, since the browser keeps scanning the network; dispose the
    /// subscription when the UI it feeds goes away.
    /// </remarks>
    public async ValueTask<ButilSubscription?> WatchAvailability(string[] urls, Action<bool> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _availabilityHandlers[id] = handler;

        var watching = await js.Invoke<bool>("BitButil.presentation.watchAvailability", id, urls, DotNetRef, AvailabilityMethodName);
        if (watching is false)
        {
            _availabilityHandlers.TryRemove(id, out _);
            return null;
        }

        return new ButilSubscription(id, async () =>
        {
            _availabilityHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.presentation.cancelWatch", id);
        });
    }

    /// <summary>
    /// The receiving half: watches
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PresentationReceiver/connectionList">navigator.presentation.receiver.connectionList</see>
    /// for the controlling pages connected to this one.
    /// </summary>
    /// <param name="onConnection">Called with a handle for each controller, including those already connected when this is called.</param>
    /// <param name="onMessage">Called with each message any controller sends.</param>
    /// <param name="onStateChange">Called when any controller's connection changes state.</param>
    /// <returns>False when this page is not a presentation receiver.</returns>
    /// <remarks>
    /// Call it once, early: a controller that connects before the page is watching still shows up
    /// (the list is not a stream of events, it is a list), but a message sent before then is lost.
    /// A presentation can have more than one controller, so expect the callback more than once.
    /// </remarks>
    public ValueTask<bool> WatchReceiverConnections(Action<PresentationConnectionHandle> onConnection,
                                                    Action<PresentationMessage>? onMessage = null,
                                                    Action<PresentationConnectionState>? onStateChange = null)
    {
        ArgumentNullException.ThrowIfNull(onConnection);

        _onReceiverConnection = onConnection;
        _receiverMessageHandler = onMessage;
        _receiverStateHandler = onStateChange;

        return js.Invoke<bool>("BitButil.presentation.watchReceiver", DotNetRef, ConnectionMethodName, MessageMethodName, StateMethodName);
    }

    /// <summary>
    /// On scope/circuit teardown, closes every connection whose handle was never disposed and cancels
    /// every availability watch. Presentations themselves keep running on their screens - closing is
    /// not terminating.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _messageHandlers.Clear();
            _stateHandlers.Clear();
            _availabilityHandlers.Clear();
            _onReceiverConnection = null;
            await js.InvokeVoid("BitButil.presentation.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }

    private void Register(Guid id, Action<PresentationMessage>? onMessage, Action<PresentationConnectionState>? onStateChange)
    {
        if (onMessage is not null) _messageHandlers[id] = onMessage;
        if (onStateChange is not null) _stateHandlers[id] = onStateChange;
    }

    private void Unregister(Guid id)
    {
        _messageHandlers.TryRemove(id, out _);
        _stateHandlers.TryRemove(id, out _);
    }

    private PresentationConnectionHandle? ToHandle(Guid id, PresentationConnectionJsInfo? info)
    {
        if (info is null)
        {
            Unregister(id);
            return null;
        }

        return new PresentationConnectionHandle(js, id, info.ConnectionId, info.Url, () => Unregister(id));
    }

    internal static PresentationConnectionState ToConnectionState(string? raw) => raw switch
    {
        "connecting" => PresentationConnectionState.Connecting,
        "connected" => PresentationConnectionState.Connected,
        "closed" => PresentationConnectionState.Closed,
        _ => PresentationConnectionState.Terminated
    };
}
