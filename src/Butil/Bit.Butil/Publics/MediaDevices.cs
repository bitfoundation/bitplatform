using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaDevices">navigator.mediaDevices</see>.
/// </summary>
public class MediaDevices(IJSRuntime js) : IAsyncDisposable
{
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

    /// <summary>
    /// On scope/circuit teardown, stops any streams whose <see cref="MediaStreamHandle"/> was never
    /// disposed so the camera/mic hardware can't stay live after the user's session ends.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try { await js.InvokeVoid("BitButil.mediaDevices.disposeAll"); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
