using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaDevices">navigator.mediaDevices</see>.
/// </summary>
[ButilService(typeof(MediaDevices))]
public class MediaDevices(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeDisplayEnded);

    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Action> _displayEndedHandlers = new();

    // Per-instance callback reference: display-capture handles are scoped to this circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<MediaDevices>? _dotNetRef;
    private DotNetObjectReference<MediaDevices> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>
    /// Invoked from JS when a display-capture stream's video track ends. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeDisplayEnded(Guid id)
    {
        // Removed rather than read: the track fires 'ended' once, and the stream behind the handle
        // is gone afterwards, so keeping the handler alive would only pin the caller's delegate.
        if (_displayEndedHandlers.TryRemove(id, out var handler)) handler.Invoke();
    }

    /// <summary>True when the runtime exposes <c>navigator.mediaDevices</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.mediaDevices.isSupported");

    /// <summary>
    /// Lists all input/output media devices. Labels may be empty strings until the user has
    /// granted permission to a matching input.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaDeviceInfo))]
    public ValueTask<MediaDeviceInfo[]> EnumerateDevices()
        => js.Invoke<MediaDeviceInfo[]>("BitButil.mediaDevices.enumerate");

    /// <summary>
    /// Requests audio and/or video access from the user. Returns a <see cref="MediaStreamHandle"/>
    /// when the prompt is accepted, null when the user denies or the runtime can't satisfy the constraints.
    /// </summary>
    /// <param name="audio">When true, requests audio. Pass detailed constraints via <paramref name="audioConstraints"/>.</param>
    /// <param name="video">When true, requests video.</param>
    /// <param name="audioConstraints">Optional <c>MediaTrackConstraints</c>-shaped object (deviceId, sampleRate, etc.).</param>
    /// <param name="videoConstraints">Optional <c>MediaTrackConstraints</c>-shaped object (width, height, facingMode, ...).</param>
    public async ValueTask<MediaStreamHandle?> GetUserMedia(bool audio = true,
                                                            bool video = false,
                                                            object? audioConstraints = null,
                                                            object? videoConstraints = null)
    {
        if (!audio && !video) throw new ArgumentException("At least one of audio/video must be true.");
        var id = Guid.NewGuid();
        var ok = await js.Invoke<bool>("BitButil.mediaDevices.getUserMedia",
            id, audio, video, audioConstraints, videoConstraints);
        return ok ? new MediaStreamHandle(js, id) : null;
    }

    /// <summary>True when the runtime exposes <c>navigator.mediaDevices.getDisplayMedia</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsDisplayCaptureSupported() => js.Invoke<bool>("BitButil.mediaDevices.isDisplaySupported");

    /// <summary>
    /// Prompts the user to pick a screen, window or tab to capture
    /// (<see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaDevices/getDisplayMedia">Screen Capture API</see>).
    /// Returns a <see cref="MediaStreamHandle"/> when the picker is accepted, null when the user
    /// dismisses it or the embedder blocks display capture.
    /// </summary>
    /// <param name="audio">When true, also asks for the selected surface's audio. Browsers only offer it for some surfaces (tab, and system audio on Windows).</param>
    /// <param name="options">Optional <see cref="DisplayMediaOptions"/> shaping the picker (which surface types to offer, whether this tab is listed, ...).</param>
    /// <param name="videoConstraints">Optional <c>MediaTrackConstraints</c>-shaped object for the video track (frameRate, width, ...).</param>
    /// <param name="onEnded">Called once if the user ends the share from the browser's own "Stop sharing" bar rather than through your UI.</param>
    /// <remarks>
    /// Must be called from a user-gesture handler. Unlike <see cref="GetUserMedia"/> this never
    /// grants a persistent permission - every call shows the picker again.
    /// </remarks>
    [DynamicDependency(nameof(InvokeDisplayEnded), typeof(MediaDevices))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DisplayMediaOptions))]
    public async ValueTask<MediaStreamHandle?> GetDisplayMedia(bool audio = false,
                                                               DisplayMediaOptions? options = null,
                                                               object? videoConstraints = null,
                                                               Action? onEnded = null)
    {
        var id = Guid.NewGuid();
        var ok = await js.Invoke<bool>("BitButil.mediaDevices.getDisplayMedia", id, audio, videoConstraints, options);
        if (ok is false) return null;

        if (onEnded is not null)
        {
            _displayEndedHandlers[id] = onEnded;
            await js.InvokeVoid("BitButil.mediaDevices.onDisplayEnded", id, DotNetRef, InvokeMethodName);
        }

        return new MediaStreamHandle(js, id);
    }

    /// <summary>
    /// What the user actually picked: the surface kind and the track's negotiated size and frame
    /// rate. Null when the stream behind <paramref name="stream"/> is already gone.
    /// </summary>
    /// <param name="stream">A handle returned by <see cref="GetDisplayMedia"/> (or <see cref="GetUserMedia"/>).</param>
    /// <remarks>
    /// The browser decides the final constraints, so this can differ from what was requested - read
    /// it rather than assuming the request was honoured.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DisplayMediaSettings))]
    public ValueTask<DisplayMediaSettings?> GetDisplaySettings(MediaStreamHandle stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return js.Invoke<DisplayMediaSettings?>("BitButil.mediaDevices.getDisplaySettings", stream.Id);
    }

    /// <summary>
    /// On scope/circuit teardown, stops any streams whose <see cref="MediaStreamHandle"/> was never
    /// disposed so the camera/mic hardware can't stay live after the user's session ends.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _displayEndedHandlers.Clear();
            await js.InvokeVoid("BitButil.mediaDevices.disposeAll");
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
