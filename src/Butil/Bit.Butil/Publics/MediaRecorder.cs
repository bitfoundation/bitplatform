using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaStream_Recording_API">MediaStream Recording API</see>
/// (<c>MediaRecorder</c>): records a live <see cref="MediaStreamHandle"/> - a camera, a microphone
/// or a screen share - into a container the browser can encode.
/// </summary>
/// <remarks>
/// Get the stream from <see cref="MediaDevices.GetUserMedia"/> or
/// <see cref="MediaDevices.GetDisplayMedia"/> first; a recorder only ever consumes a stream
/// somebody else opened, and stopping the recorder does not stop that stream.
/// <br/>
/// Which containers and codecs are available differs per engine, so pick one with
/// <see cref="GetSupportedTypes"/> rather than hard-coding a MIME type.
/// </remarks>
public class MediaRecorder(IJSRuntime js) : IAsyncDisposable
{
    internal const string DataMethodName = nameof(InvokeRecorderData);
    internal const string ErrorMethodName = nameof(InvokeRecorderError);

    /// <summary>
    /// The containers worth probing on today's engines, most-preferred first. Chromium and Firefox
    /// speak WebM, Safari only speaks MP4, so a list rather than a single default is the only thing
    /// that works everywhere.
    /// </summary>
    public static readonly string[] CommonVideoTypes =
    [
        "video/webm;codecs=vp9,opus",
        "video/webm;codecs=vp8,opus",
        "video/webm",
        "video/mp4;codecs=avc1",
        "video/mp4",
    ];

    /// <summary>Audio-only containers worth probing, most-preferred first.</summary>
    public static readonly string[] CommonAudioTypes =
    [
        "audio/webm;codecs=opus",
        "audio/webm",
        "audio/ogg;codecs=opus",
        "audio/mp4",
        "audio/mpeg",
    ];

    private readonly ConcurrentDictionary<Guid, Action<byte[]>> _dataHandlers = new();
    private readonly ConcurrentDictionary<Guid, Action<string>> _errorHandlers = new();

    // Per-instance callback reference (see Keyboard): recorders are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<MediaRecorder>? _dotNetRef;
    private DotNetObjectReference<MediaRecorder> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>MediaRecorder</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.mediaRecorder.isSupported");

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaRecorder/isTypeSupported_static">MediaRecorder.isTypeSupported()</see>:
    /// true when this engine can record the given container/codec string.
    /// </summary>
    /// <param name="mimeType">A container, optionally with codecs, e.g. <c>"video/webm;codecs=vp9,opus"</c>.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsTypeSupported(string mimeType)
        => js.Invoke<bool>("BitButil.mediaRecorder.isTypeSupported", mimeType);

    /// <summary>
    /// Filters <paramref name="candidates"/> down to the ones this engine can actually record,
    /// preserving the order you passed them in - so the first entry is your best available choice.
    /// </summary>
    /// <param name="candidates">The MIME types to probe. Defaults to <see cref="CommonVideoTypes"/>.</param>
    /// <returns>An empty array when nothing matches, or when <c>MediaRecorder</c> is unavailable.</returns>
    public ValueTask<string[]> GetSupportedTypes(string[]? candidates = null)
        => js.Invoke<string[]>("BitButil.mediaRecorder.supportedTypes", (object)(candidates ?? CommonVideoTypes));

    /// <summary>
    /// Invoked from JS for each recorded slice when the caller asked for chunks. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(DataMethodName)]
    public void InvokeRecorderData(Guid id, byte[] data)
    {
        if (_dataHandlers.TryGetValue(id, out var handler)) handler.Invoke(data);
    }

    /// <summary>
    /// Invoked from JS when the recorder raises an error. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeRecorderError(Guid id, string message)
    {
        if (_errorHandlers.TryGetValue(id, out var handler)) handler.Invoke(message);
    }

    /// <summary>
    /// Starts recording <paramref name="stream"/>. Use the returned handle to pause, resume and
    /// finally stop - stopping is what produces the recorded bytes.
    /// </summary>
    /// <param name="stream">A live stream from <see cref="MediaDevices.GetUserMedia"/> or <see cref="MediaDevices.GetDisplayMedia"/>.</param>
    /// <param name="options">Container and bitrate hints. Leave null to let the browser choose.</param>
    /// <param name="onData">
    /// Called with each slice as it is encoded, so a long recording can be uploaded while it runs
    /// instead of being held in memory. Only fires when <paramref name="timesliceMs"/> is set, or
    /// when you call <see cref="MediaRecordingHandle.RequestData"/>.
    /// </param>
    /// <param name="timesliceMs">How often to emit a slice, in milliseconds. Null records one slice for the whole take.</param>
    /// <param name="onError">Called when the recorder fails mid-take. The recording ends at that point.</param>
    /// <returns>A handle, or null when the runtime has no <c>MediaRecorder</c>, the stream is already
    /// stopped, or the requested container isn't supported.</returns>
    [DynamicDependency(nameof(InvokeRecorderData), typeof(MediaRecorder))]
    [DynamicDependency(nameof(InvokeRecorderError), typeof(MediaRecorder))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaRecorderOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RecordedMedia))]
    public async ValueTask<MediaRecordingHandle?> Start(MediaStreamHandle stream,
                                                        MediaRecorderOptions? options = null,
                                                        Action<byte[]>? onData = null,
                                                        int? timesliceMs = null,
                                                        Action<string>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var id = Guid.NewGuid();
        if (onData is not null) _dataHandlers[id] = onData;
        if (onError is not null) _errorHandlers[id] = onError;

        var started = await js.Invoke<bool>("BitButil.mediaRecorder.start",
            id, stream.Id, options, timesliceMs, DotNetRef, onData is not null);

        if (started is false)
        {
            _dataHandlers.TryRemove(id, out _);
            _errorHandlers.TryRemove(id, out _);
            return null;
        }

        return new MediaRecordingHandle(js, id, () =>
        {
            _dataHandlers.TryRemove(id, out _);
            _errorHandlers.TryRemove(id, out _);
        });
    }

    /// <summary>
    /// Releases an object URL returned by <see cref="MediaRecordingHandle.StopAndCreateObjectUrl"/>.
    /// Safe to call more than once, and safe with a null/empty url.
    /// </summary>
    /// <param name="objectUrl">The URL from <see cref="RecordedMedia.ObjectUrl"/>.</param>
    /// <remarks>
    /// The same call as <see cref="MediaRecordingHandle.RevokeObjectUrl"/>, but reachable after the
    /// handle that produced the URL is gone - which is the normal case, since stopping a recording
    /// is what retires its handle.
    /// </remarks>
    public ValueTask RevokeObjectUrl(string? objectUrl)
        => string.IsNullOrEmpty(objectUrl)
            ? default
            : js.InvokeVoid("BitButil.mediaRecorder.revoke", objectUrl);

    /// <summary>
    /// On scope/circuit teardown, stops any recording whose <see cref="MediaRecordingHandle"/> was
    /// never disposed so an abandoned recorder can't keep encoding after the session ends.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _dataHandlers.Clear();
            _errorHandlers.Clear();
            await js.InvokeVoid("BitButil.mediaRecorder.disposeAll");
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
