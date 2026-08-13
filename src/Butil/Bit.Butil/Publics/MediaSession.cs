using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Media_Session_API">Media Session API</see>:
/// tells the platform what your app is playing, so the OS lock screen, notification shade, headset
/// buttons and media keys can show and control it.
/// </summary>
/// <remarks>
/// This only describes and remote-controls playback - it never plays anything. Keep it in step
/// with whatever is actually producing sound: set the metadata when a track starts, update
/// <see cref="SetPlaybackState"/> on play/pause, and let the handlers registered through
/// <see cref="SetActionHandler"/> drive your player.
/// <br/>
/// Most platforms only surface the session once audio is actually playing, so setting metadata on
/// a silent page usually shows nothing.
/// </remarks>
public class MediaSession(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeMediaSessionAction);

    private readonly ConcurrentDictionary<string, Action<MediaSessionActionDetails>> _handlers = new();

    // Per-instance callback reference (see Keyboard): handlers are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<MediaSession>? _dotNetRef;
    private DotNetObjectReference<MediaSession> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.mediaSession</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.mediaSession.isSupported");

    /// <summary>
    /// Describes what is playing: the title, artist, album and artwork the platform shows.
    /// </summary>
    /// <param name="metadata">The track description, or null to clear it.</param>
    /// <returns>False when the runtime has no media session, or the artwork was rejected.</returns>
    /// <remarks>
    /// Artwork URLs must be reachable by the browser; a bad one rejects the whole metadata object,
    /// which is reported here as false rather than as an exception.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaMetadata))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaArtwork))]
    public ValueTask<bool> SetMetadata(MediaMetadata? metadata)
        => js.Invoke<bool>("BitButil.mediaSession.setMetadata", metadata);

    /// <summary>
    /// Tells the platform whether playback is running, so it can show the right play/pause icon.
    /// </summary>
    /// <param name="state">The current state.</param>
    public ValueTask SetPlaybackState(MediaSessionPlaybackState state)
        => js.InvokeVoid("BitButil.mediaSession.setPlaybackState", state switch
        {
            MediaSessionPlaybackState.Playing => "playing",
            MediaSessionPlaybackState.Paused => "paused",
            _ => "none",
        });

    /// <summary>
    /// Publishes the playback position so the platform can draw an accurate scrubber and let the
    /// user seek from outside your page.
    /// </summary>
    /// <param name="durationSeconds">The track's total length in seconds.</param>
    /// <param name="positionSeconds">The current position in seconds. Clamped into the track's range.</param>
    /// <param name="playbackRate">The current rate; 1 is normal speed. Values of 0 are treated as 1, which the spec rejects.</param>
    /// <returns>False when the runtime has no <c>setPositionState</c>, or the values were rejected.</returns>
    /// <remarks>
    /// Call this whenever you seek or the rate changes - the platform extrapolates the position
    /// between updates, so it does not need one per frame.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> SetPositionState(double durationSeconds, double positionSeconds, double playbackRate = 1)
        => js.Invoke<bool>("BitButil.mediaSession.setPositionState", durationSeconds, playbackRate, positionSeconds);

    /// <summary>Drops the published position, e.g. when playback stops entirely.</summary>
    public ValueTask ClearPositionState() => js.InvokeVoid("BitButil.mediaSession.clearPositionState");

    /// <summary>
    /// Invoked from JS when the platform triggers a media action. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeMediaSessionAction(string action, MediaSessionActionDetails details)
    {
        if (_handlers.TryGetValue(action, out var handler)) handler.Invoke(details);
    }

    /// <summary>
    /// Registers a handler for a platform control - a media key, a headset button, a lock-screen
    /// button. Registering is also what makes that control appear.
    /// </summary>
    /// <param name="action">Which control to handle.</param>
    /// <param name="handler">Called when the user triggers it. For seek actions, read the offset from the details.</param>
    /// <returns>False when the engine doesn't implement this action - the usual way to discover that <see cref="MediaSessionAction.SeekTo"/> or the skip actions are unavailable.</returns>
    /// <remarks>
    /// Registering the same action twice replaces the previous handler rather than adding a second.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(nameof(InvokeMediaSessionAction), typeof(MediaSession))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaSessionActionDetails))]
    public async ValueTask<bool> SetActionHandler(MediaSessionAction action, Action<MediaSessionActionDetails> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var name = ToName(action);
        _handlers[name] = handler;

        var ok = await js.Invoke<bool>("BitButil.mediaSession.setActionHandler", DotNetRef, name);
        if (ok is false) _handlers.TryRemove(name, out _);
        return ok;
    }

    /// <summary>Removes a handler, which also hides the corresponding platform control.</summary>
    /// <param name="action">The action to stop handling.</param>
    public ValueTask ClearActionHandler(MediaSessionAction action)
    {
        var name = ToName(action);
        _handlers.TryRemove(name, out _);
        return js.InvokeVoid("BitButil.mediaSession.clearActionHandler", name);
    }

    private static string ToName(MediaSessionAction action) => action switch
    {
        MediaSessionAction.Play => "play",
        MediaSessionAction.Pause => "pause",
        MediaSessionAction.Stop => "stop",
        MediaSessionAction.SeekBackward => "seekbackward",
        MediaSessionAction.SeekForward => "seekforward",
        MediaSessionAction.SeekTo => "seekto",
        MediaSessionAction.PreviousTrack => "previoustrack",
        MediaSessionAction.NextTrack => "nexttrack",
        MediaSessionAction.SkipAd => "skipad",
        MediaSessionAction.ToggleMicrophone => "togglemicrophone",
        MediaSessionAction.ToggleCamera => "togglecamera",
        MediaSessionAction.HangUp => "hangup",
        _ => "play",
    };

    /// <summary>
    /// On scope/circuit teardown, clears every handler this instance registered and resets the
    /// session, so a stale player can't keep a lock-screen control alive after the page moved on.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.mediaSession.disposeAll");
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
