using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A recording in progress, returned by <see cref="MediaRecorder.Start"/>. Stopping it is what
/// produces the recorded media; disposing without stopping throws the take away.
/// </summary>
public sealed class MediaRecordingHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Action _onFinished;
    private bool _finished;

    internal MediaRecordingHandle(IJSRuntime js, Guid id, Action onFinished)
    {
        _js = js;
        Id = id;
        _onFinished = onFinished;
    }

    /// <summary>The internal recording id.</summary>
    public Guid Id { get; }

    /// <summary>
    /// <c>"recording"</c>, <c>"paused"</c>, or <c>"inactive"</c> once the recording has stopped or
    /// failed.
    /// </summary>
    public ValueTask<string> GetState() => _js.Invoke<string>("BitButil.mediaRecorder.state", Id);

    /// <summary>
    /// The container the browser settled on, which can differ from the one that was requested.
    /// Empty once the recording is over - read it from <see cref="RecordedMedia.MimeType"/> then.
    /// </summary>
    public ValueTask<string> GetMimeType() => _js.Invoke<string>("BitButil.mediaRecorder.mimeType", Id);

    /// <summary>Pauses encoding without ending the take. No-op unless currently recording.</summary>
    public ValueTask Pause() => _js.InvokeVoid("BitButil.mediaRecorder.pause", Id);

    /// <summary>Resumes a paused take. No-op unless currently paused.</summary>
    public ValueTask Resume() => _js.InvokeVoid("BitButil.mediaRecorder.resume", Id);

    /// <summary>
    /// Emits everything captured so far as a slice - delivered to the <c>onData</c> callback passed
    /// to <see cref="MediaRecorder.Start"/> - and keeps recording.
    /// </summary>
    public ValueTask RequestData() => _js.InvokeVoid("BitButil.mediaRecorder.requestData", Id);

    /// <summary>
    /// Stops the recording and returns the encoded bytes.
    /// </summary>
    /// <returns>The recording, or null when it was already stopped or disposed.</returns>
    /// <remarks>
    /// The whole take crosses the interop boundary, so for anything long prefer
    /// <see cref="StopAndCreateObjectUrl"/>, or pass an <c>onData</c> callback to
    /// <see cref="MediaRecorder.Start"/> and upload the slices as they arrive.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RecordedMedia))]
    public ValueTask<RecordedMedia?> Stop() => StopCore(asObjectUrl: false);

    /// <summary>
    /// Stops the recording and returns it as a <c>blob:</c> URL, leaving the bytes in the browser -
    /// the cheap way to hand a take straight to a media element for playback or download.
    /// </summary>
    /// <returns>The recording, or null when it was already stopped or disposed.</returns>
    /// <remarks>
    /// The URL pins the recording in memory until <see cref="RevokeObjectUrl"/> is called.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RecordedMedia))]
    public ValueTask<RecordedMedia?> StopAndCreateObjectUrl() => StopCore(asObjectUrl: true);

    private async ValueTask<RecordedMedia?> StopCore(bool asObjectUrl)
    {
        if (_finished) return null;
        _finished = true;
        try
        {
            return await _js.Invoke<RecordedMedia?>("BitButil.mediaRecorder.stop", Id, asObjectUrl);
        }
        finally
        {
            _onFinished();
        }
    }

    /// <summary>
    /// Releases an object URL returned by <see cref="StopAndCreateObjectUrl"/>. Safe to call more
    /// than once, and safe with a null/empty url.
    /// </summary>
    /// <param name="objectUrl">The URL from <see cref="RecordedMedia.ObjectUrl"/>.</param>
    public ValueTask RevokeObjectUrl(string? objectUrl)
        => string.IsNullOrEmpty(objectUrl)
            ? default
            : _js.InvokeVoid("BitButil.mediaRecorder.revoke", objectUrl);

    /// <summary>
    /// Abandons the recording, discarding anything captured so far. Stopping first is what keeps
    /// the take - this is the cleanup path, and is a no-op once the recording has been stopped.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_finished) return;
        _finished = true;
        _onFinished();
        try { await _js.InvokeVoid("BitButil.mediaRecorder.cancel", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
