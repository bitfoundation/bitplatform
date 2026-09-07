using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to a <see href="https://developer.mozilla.org/en-US/docs/Web/API/SourceBuffer">SourceBuffer</see>
/// created by <see cref="MediaSourceHandle.AddSourceBuffer"/> - the thing an adaptive player pushes
/// its downloaded segments into.
/// </summary>
/// <remarks>
/// A SourceBuffer can only do one thing at a time: appending or removing while it is already busy is
/// an <c>InvalidStateError</c> in the raw API. Butil serializes the calls per buffer instead, so
/// concurrent <see cref="Append"/> calls from .NET queue up and complete in order, and every one of
/// them resolves only once the browser has finished with it.
/// <br/>
/// The first append has to be the stream's initialization segment; media segments follow.
/// </remarks>
public sealed class SourceBufferHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _sourceId;
    private readonly Guid _id;
    private bool _disposed;

    internal SourceBufferHandle(IJSRuntime js, Guid sourceId, Guid id, string mimeType)
    {
        _js = js;
        _sourceId = sourceId;
        _id = id;
        MimeType = mimeType;
    }

    /// <summary>The internal buffer id.</summary>
    public Guid Id => _id;

    /// <summary>The container and codecs this buffer was created for.</summary>
    public string MimeType { get; private set; }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SourceBuffer/appendBuffer">SourceBuffer.appendBuffer()</see>:
    /// hands the buffer one segment and completes once the browser has taken it.
    /// </summary>
    /// <param name="data">An initialization segment (the first append) or a media segment.</param>
    /// <returns>What became of the append - see <see cref="SourceBufferAppendStatus"/>.</returns>
    /// <remarks>
    /// <see cref="SourceBufferAppendStatus.QuotaExceeded"/> is not a failure of the app: the buffer
    /// filled up, and the accepted response is to <see cref="Remove"/> the already-played range and
    /// append the same bytes again. Treating it as fatal is the classic way to make a long playback
    /// stall.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async ValueTask<SourceBufferAppendStatus> Append(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ToStatus(await _js.Invoke<string>("BitButil.mediaSource.append", _sourceId, _id, data));
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SourceBuffer/remove">SourceBuffer.remove()</see>:
    /// drops the media between two points on the timeline, in seconds.
    /// </summary>
    /// <param name="start">Where to start removing.</param>
    /// <param name="end">Where to stop - use <see cref="double.PositiveInfinity"/> to remove everything after <paramref name="start"/>.</param>
    /// <returns>True once the removal is complete.</returns>
    /// <remarks>
    /// This is the eviction half of a player's buffer management: keep a window around the current
    /// time, and remove what is behind it so the buffer never fills up.
    /// </remarks>
    public async ValueTask<bool> Remove(double start, double end)
        => ToStatus(await _js.Invoke<string>("BitButil.mediaSource.remove", _sourceId, _id, start, end)) == SourceBufferAppendStatus.Success;

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SourceBuffer/abort">SourceBuffer.abort()</see>:
    /// cancels whatever the buffer is currently doing and resets its parser state.
    /// </summary>
    /// <remarks>
    /// What a quality switch calls before appending segments of the new representation, so a
    /// half-parsed segment of the old one can't corrupt the buffer.
    /// </remarks>
    public ValueTask Abort() => _js.InvokeVoid("BitButil.mediaSource.abort", _sourceId, _id);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SourceBuffer/changeType">SourceBuffer.changeType()</see>:
    /// re-points an existing buffer at a different container or codec.
    /// </summary>
    /// <param name="mimeType">The new container with its codecs.</param>
    /// <returns>False when the engine doesn't implement it, or the new type is unsupported.</returns>
    /// <remarks>
    /// The alternative to tearing the buffer down and rebuilding it when a stream switches codec -
    /// which is what makes a seamless switch between, say, AVC and HEVC representations possible.
    /// The next append after a successful call has to be an initialization segment of the new type.
    /// </remarks>
    public async ValueTask<bool> ChangeType(string mimeType)
    {
        var changed = await _js.Invoke<bool>("BitButil.mediaSource.changeType", _sourceId, _id, mimeType);
        if (changed) MimeType = mimeType;

        return changed;
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SourceBuffer/mode">SourceBuffer.mode</see>:
    /// whether appended segments keep their own timestamps or are laid end to end.
    /// </summary>
    /// <param name="mode">The interpretation to use for subsequent appends.</param>
    /// <returns>False when the buffer is mid-append, or the source is no longer open.</returns>
    public ValueTask<bool> SetMode(SourceBufferMode mode)
        => _js.Invoke<bool>("BitButil.mediaSource.setMode", _sourceId, _id, mode == SourceBufferMode.Sequence ? "sequence" : "segments");

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SourceBuffer/timestampOffset">SourceBuffer.timestampOffset</see>:
    /// shifts every subsequently appended segment along the timeline, in seconds.
    /// </summary>
    /// <param name="seconds">How far to move the segments; may be negative.</param>
    /// <returns>False when the buffer is mid-append, or the source is no longer open.</returns>
    /// <remarks>
    /// How an ad or a second stream whose timestamps start at zero is spliced into the middle of a
    /// timeline that is already running.
    /// </remarks>
    public ValueTask<bool> SetTimestampOffset(double seconds)
        => _js.Invoke<bool>("BitButil.mediaSource.setTimestampOffset", _sourceId, _id, seconds);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SourceBuffer/appendWindowStart">appendWindowStart</see>
    /// and <see href="https://developer.mozilla.org/en-US/docs/Web/API/SourceBuffer/appendWindowEnd">appendWindowEnd</see>:
    /// media outside this window is dropped as it is appended.
    /// </summary>
    /// <param name="start">Where the window opens, in seconds.</param>
    /// <param name="end">Where it closes - <see cref="double.PositiveInfinity"/> for no upper bound.</param>
    /// <returns>False when the values were rejected, the buffer is mid-append, or the source is no longer open.</returns>
    /// <remarks>
    /// The clean way to trim a segment that overlaps a splice point, without re-encoding it.
    /// </remarks>
    public ValueTask<bool> SetAppendWindow(double start, double end)
        => _js.Invoke<bool>("BitButil.mediaSource.setAppendWindow", _sourceId, _id, start, end);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SourceBuffer/buffered">SourceBuffer.buffered</see>:
    /// the stretches of media this buffer currently holds.
    /// </summary>
    /// <returns>The ranges in timeline order; empty when nothing has been appended yet.</returns>
    /// <remarks>
    /// More than one range means there is a gap that playback will stop at. Comparing the end of the
    /// range holding the current time against that time is how a player decides whether it is far
    /// enough ahead to wait, or needs to fetch now.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BufferedTimeRange))]
    public ValueTask<BufferedTimeRange[]> GetBuffered()
        => _js.Invoke<BufferedTimeRange[]>("BitButil.mediaSource.buffered", _sourceId, _id);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SourceBuffer/updating">SourceBuffer.updating</see>:
    /// true while the browser is still working through an append or a removal.
    /// </summary>
    /// <remarks>
    /// Informational here rather than load-bearing: <see cref="Append"/> and <see cref="Remove"/>
    /// already wait their turn, so there is no need to poll this before calling them.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsUpdating() => _js.Invoke<bool>("BitButil.mediaSource.isUpdating", _sourceId, _id);

    /// <summary>
    /// Removes this buffer from its media source, discarding the media it holds. Calling it again
    /// does nothing.
    /// </summary>
    /// <remarks>
    /// Disposing the owning <see cref="MediaSourceHandle"/> drops every buffer as well, so this is
    /// only needed when a player wants to rebuild one buffer while the rest keep playing.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.mediaSource.removeSourceBuffer", _sourceId, _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }

    private static SourceBufferAppendStatus ToStatus(string? raw) => raw switch
    {
        "success" => SourceBufferAppendStatus.Success,
        "quota-exceeded" => SourceBufferAppendStatus.QuotaExceeded,
        "aborted" => SourceBufferAppendStatus.Aborted,
        "closed" => SourceBufferAppendStatus.Closed,
        _ => SourceBufferAppendStatus.Failed
    };
}
