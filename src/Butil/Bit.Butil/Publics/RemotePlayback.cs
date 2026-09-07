using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Remote_Playback_API">Remote Playback API</see>:
/// hands a single <c>&lt;video&gt;</c> or <c>&lt;audio&gt;</c> element off to a TV, a speaker or a
/// cast receiver on the network, and reports what happens to it there.
/// </summary>
/// <remarks>
/// This is the element-level half of casting: the remote device fetches and plays the media itself,
/// so the page stops decoding and the element becomes a remote control. For putting arbitrary
/// <em>page</em> content on a second screen, see <see cref="Presentation"/>.
/// <br/>
/// The two calls that matter come as a pair: <see cref="WatchAvailability"/> tells you whether there
/// is anything to cast to (so the button can be hidden when there isn't), and <see cref="Prompt"/>
/// opens the browser's own device picker - which needs a user gesture and can only be answered by
/// the user.
/// </remarks>
[ButilService(typeof(RemotePlayback))]
public class RemotePlayback(IJSRuntime js) : IAsyncDisposable
{
    internal const string AvailabilityMethodName = nameof(InvokeRemotePlaybackAvailability);
    internal const string StateMethodName = nameof(InvokeRemotePlaybackState);

    private readonly ConcurrentDictionary<Guid, Action<bool>> _availabilityHandlers = new();
    private readonly ConcurrentDictionary<Guid, Action<RemotePlaybackState>> _stateHandlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<RemotePlayback>? _dotNetRef;
    private DotNetObjectReference<RemotePlayback> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when media elements expose a <c>remote</c> object.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.remotePlayback.isSupported");

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/RemotePlayback/state">RemotePlayback.state</see>:
    /// whether this element is currently playing somewhere else.
    /// </summary>
    /// <param name="mediaElement">The element to ask about.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async ValueTask<RemotePlaybackState> GetState(ElementReference mediaElement)
        => ToState(await js.Invoke<string>("BitButil.remotePlayback.state", mediaElement));

    /// <summary>
    /// Sets the element's
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLMediaElement/disableRemotePlayback">disableRemotePlayback</see>
    /// property, which is what removes the cast button the browser draws in its own controls.
    /// </summary>
    /// <param name="mediaElement">The element to change.</param>
    /// <param name="disabled">True to opt this element out of remote playback entirely.</param>
    /// <returns>False when the element is gone.</returns>
    /// <remarks>
    /// Worth setting on media that must not leave the device - a DRM-protected stream whose licence
    /// forbids it, or a local preview - since the browser otherwise offers casting on its own.
    /// </remarks>
    public ValueTask<bool> SetDisabled(ElementReference mediaElement, bool disabled)
        => js.Invoke<bool>("BitButil.remotePlayback.setDisabled", mediaElement, disabled);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/RemotePlayback/prompt">RemotePlayback.prompt()</see>:
    /// opens the browser's device picker for this element.
    /// </summary>
    /// <param name="mediaElement">The element to cast.</param>
    /// <returns>False when the user dismissed the picker, no device was found, or there was no user gesture behind the call.</returns>
    /// <remarks>
    /// Must be called from a user-gesture handler such as a click. A true here means the picker was
    /// answered, not that playback has started - watch <see cref="SubscribeStateChange"/> for that.
    /// </remarks>
    public ValueTask<bool> Prompt(ElementReference mediaElement)
        => js.Invoke<bool>("BitButil.remotePlayback.prompt", mediaElement);

    /// <summary>
    /// Invoked from JS when the availability of remote devices changes. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(AvailabilityMethodName)]
    public void InvokeRemotePlaybackAvailability(Guid id, bool available)
    {
        if (_availabilityHandlers.TryGetValue(id, out var handler)) handler.Invoke(available);
    }

    /// <summary>
    /// Invoked from JS when an element connects to or disconnects from a remote device. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(StateMethodName)]
    public void InvokeRemotePlaybackState(Guid id, string state)
    {
        if (_stateHandlers.TryGetValue(id, out var handler)) handler.Invoke(ToState(state));
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/RemotePlayback/watchAvailability">RemotePlayback.watchAvailability()</see>:
    /// whether there is any device this element could be cast to.
    /// </summary>
    /// <param name="mediaElement">The element to watch.</param>
    /// <param name="handler">Called with the current answer straight away, and again whenever it changes.</param>
    /// <returns>A subscription, or <c>null</c> when the API is unavailable or the element opted out.</returns>
    /// <remarks>
    /// Watching costs battery - the browser keeps scanning the network - so drop the subscription
    /// when the UI it feeds goes away. That is what disposing the returned token does.
    /// </remarks>
    public async ValueTask<ButilSubscription?> WatchAvailability(ElementReference mediaElement, Action<bool> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _availabilityHandlers[id] = handler;

        var watching = await js.Invoke<bool>("BitButil.remotePlayback.watchAvailability", id, mediaElement, DotNetRef, AvailabilityMethodName);
        if (watching is false)
        {
            _availabilityHandlers.TryRemove(id, out _);
            return null;
        }

        return new ButilSubscription(id, async () =>
        {
            _availabilityHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.remotePlayback.cancelWatch", id);
        });
    }

    /// <summary>
    /// Watches an element connecting to and disconnecting from a remote device - including when the
    /// user ends the cast from the device itself, which is the only way to learn about that.
    /// </summary>
    /// <param name="mediaElement">The element to watch.</param>
    /// <param name="handler">Called on each transition.</param>
    /// <returns>A subscription - dispose it to detach the listeners.</returns>
    public async ValueTask<ButilSubscription> SubscribeStateChange(ElementReference mediaElement, Action<RemotePlaybackState> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _stateHandlers[id] = handler;
        await js.InvokeVoid("BitButil.remotePlayback.subscribeState", id, mediaElement, DotNetRef, StateMethodName);

        return new ButilSubscription(id, async () =>
        {
            _stateHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.remotePlayback.unsubscribeState", id);
        });
    }

    /// <summary>
    /// On scope/circuit teardown, cancels every availability watch and detaches every state listener
    /// whose <see cref="ButilSubscription"/> was never disposed. Media already playing remotely is
    /// left alone - it is the user's.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _availabilityHandlers.Clear();
            _stateHandlers.Clear();
            await js.InvokeVoid("BitButil.remotePlayback.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }

    private static RemotePlaybackState ToState(string? raw) => raw switch
    {
        "connecting" => RemotePlaybackState.Connecting,
        "connected" => RemotePlaybackState.Connected,
        _ => RemotePlaybackState.Disconnected
    };
}
