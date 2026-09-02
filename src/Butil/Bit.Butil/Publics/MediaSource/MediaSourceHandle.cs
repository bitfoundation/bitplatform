using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to a <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaSource">MediaSource</see>
/// attached to a media element by <see cref="Bit.Butil.MediaSource.Open"/>. Dispose it to detach the
/// source and release the media it is holding.
/// </summary>
/// <remarks>
/// A handle is handed out already <see cref="MediaSourceReadyState.Open"/>, so
/// <see cref="AddSourceBuffer"/> can be called straight away. It stops being open when the element
/// is torn down or the source is disposed, and every call made afterwards answers with its "nothing
/// happened" result rather than throwing.
/// </remarks>
public sealed class MediaSourceHandle : IAsyncDisposable
{
    internal const string ReadyStateMethodName = nameof(InvokeMediaSourceReadyState);

    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private readonly ConcurrentDictionary<Guid, Action<MediaSourceReadyState>> _handlers = new();

    private DotNetObjectReference<MediaSourceHandle>? _dotNetRef;
    private DotNetObjectReference<MediaSourceHandle> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    private bool _disposed;

    internal MediaSourceHandle(IJSRuntime js, Guid id) { _js = js; _id = id; }

    /// <summary>The internal media-source id; buffers created from this handle are keyed under it.</summary>
    public Guid Id => _id;

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaSource/addSourceBuffer">MediaSource.addSourceBuffer()</see>:
    /// creates a buffer that accepts segments of one container and codec combination.
    /// </summary>
    /// <param name="mimeType">The container with its codecs, e.g. <c>video/mp4;codecs="avc1.42E01E,mp4a.40.2"</c>.</param>
    /// <returns>The buffer handle, or <c>null</c> when the type is unsupported or the source is no longer open.</returns>
    /// <remarks>
    /// One buffer per set of tracks that share a container: muxed audio and video is one buffer,
    /// separate audio and video representations are two. The first thing appended to a buffer has to
    /// be the stream's initialization segment - a media segment appended before it is rejected.
    /// <br/>
    /// Probe the type with <see cref="Bit.Butil.MediaSource.IsTypeSupported"/> first; an engine that
    /// can't decode it refuses the buffer rather than failing later at the append.
    /// </remarks>
    public async ValueTask<SourceBufferHandle?> AddSourceBuffer(string mimeType)
    {
        var bufferId = Guid.NewGuid();
        var added = await _js.Invoke<bool>("BitButil.mediaSource.addSourceBuffer", _id, bufferId, mimeType);

        return added ? new SourceBufferHandle(_js, _id, bufferId, mimeType) : null;
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaSource/readyState">MediaSource.readyState</see>:
    /// whether the source is attached and accepting appends.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async ValueTask<MediaSourceReadyState> GetReadyState()
        => ToReadyState(await _js.Invoke<string>("BitButil.mediaSource.readyState", _id));

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaSource/duration">MediaSource.duration</see>
    /// in seconds, or <c>null</c> while the source has no duration yet.
    /// </summary>
    /// <remarks>
    /// A source that has never been given a duration reports <c>NaN</c> in JavaScript, which is
    /// reported here as <c>null</c> rather than as a number nothing can be done with.
    /// </remarks>
    public ValueTask<double?> GetDuration() => _js.Invoke<double?>("BitButil.mediaSource.duration", _id);

    /// <summary>
    /// Sets the timeline's total length in seconds - what the element reports as its duration and
    /// what a seek bar is drawn from.
    /// </summary>
    /// <param name="seconds">The duration, or <see cref="double.PositiveInfinity"/> for a live stream of unknown length.</param>
    /// <returns>False when the source is not open, or a buffer is mid-append.</returns>
    /// <remarks>
    /// Shortening the duration below what is already buffered truncates the buffered media, so a
    /// player normally sets this once from the manifest.
    /// </remarks>
    public ValueTask<bool> SetDuration(double seconds)
        => _js.Invoke<bool>("BitButil.mediaSource.setDuration", _id, seconds);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaSource/endOfStream">MediaSource.endOfStream()</see>:
    /// declares that everything that will ever be appended has been appended.
    /// </summary>
    /// <param name="error">
    /// <see cref="MediaSourceEndOfStreamError.None"/> for a stream that finished normally, or a
    /// reason to end the element in an error state instead.
    /// </param>
    /// <returns>False when the source is not open, or a buffer is still updating.</returns>
    /// <remarks>
    /// This moves the source to <see cref="MediaSourceReadyState.Ended"/>: playback continues to the
    /// end of what is buffered and the element then fires <c>ended</c>. Appending afterwards requires
    /// re-opening the source (setting the duration, or adding a buffer, does not).
    /// </remarks>
    public ValueTask<bool> EndOfStream(MediaSourceEndOfStreamError error = MediaSourceEndOfStreamError.None)
        => _js.Invoke<bool>("BitButil.mediaSource.endOfStream", _id, error switch
        {
            MediaSourceEndOfStreamError.Network => "network",
            MediaSourceEndOfStreamError.Decode => "decode",
            _ => null
        });

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaSource/setLiveSeekableRange">MediaSource.setLiveSeekableRange()</see>:
    /// the window a live stream lets the user seek within, in seconds.
    /// </summary>
    /// <param name="start">Where the seekable window starts.</param>
    /// <param name="end">Where it ends - normally the live edge.</param>
    /// <returns>False when the engine doesn't implement it, or the source is not open.</returns>
    /// <remarks>
    /// Without this, a live stream whose duration is infinite is seekable only across what happens to
    /// be buffered. Setting the range is what makes a DVR window work.
    /// </remarks>
    public ValueTask<bool> SetLiveSeekableRange(double start, double end)
        => _js.Invoke<bool>("BitButil.mediaSource.setLiveSeekableRange", _id, start, end);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaSource/clearLiveSeekableRange">MediaSource.clearLiveSeekableRange()</see>:
    /// drops the seekable window set by <see cref="SetLiveSeekableRange"/>.
    /// </summary>
    /// <returns>False when the engine doesn't implement it, or the source is not open.</returns>
    public ValueTask<bool> ClearLiveSeekableRange()
        => _js.Invoke<bool>("BitButil.mediaSource.clearLiveSeekableRange", _id);

    /// <summary>
    /// Invoked from JS when the source opens, ends or closes. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ReadyStateMethodName)]
    public void InvokeMediaSourceReadyState(Guid id, string readyState)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(ToReadyState(readyState));
    }

    /// <summary>
    /// Watches the source's ready state - the only way to learn that the element detached it, which
    /// happens when the element's <c>src</c> changes or the element is removed from the document.
    /// </summary>
    /// <param name="handler">Called with the new state on every <c>sourceopen</c>, <c>sourceended</c> and <c>sourceclose</c>.</param>
    /// <returns>A subscription - dispose it to detach the listeners.</returns>
    public async ValueTask<ButilSubscription> Subscribe(Action<MediaSourceReadyState> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var listenerId = Guid.NewGuid();
        _handlers[listenerId] = handler;
        await _js.InvokeVoid("BitButil.mediaSource.subscribe", _id, DotNetRef, listenerId, ReadyStateMethodName);

        return new ButilSubscription(listenerId, async () =>
        {
            _handlers.TryRemove(listenerId, out _);
            await _js.InvokeVoid("BitButil.mediaSource.unsubscribe", _id, listenerId);
        });
    }

    /// <summary>
    /// Detaches the media source from its element, revokes its object URL and drops its buffers.
    /// Calling it again does nothing.
    /// </summary>
    /// <remarks>
    /// The object URL is what keeps the source - and everything buffered into it - alive, so this is
    /// the call that actually frees the memory a long playback accumulated.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            _handlers.Clear();
            await _js.InvokeVoid("BitButil.mediaSource.close", _id);
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }

    private static MediaSourceReadyState ToReadyState(string? raw) => raw switch
    {
        "open" => MediaSourceReadyState.Open,
        "ended" => MediaSourceReadyState.Ended,
        _ => MediaSourceReadyState.Closed
    };
}
