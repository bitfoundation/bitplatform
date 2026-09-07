using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// What every WebCodecs encoder and decoder handle has in common: the queue, the state, and the
/// teardown. See <see cref="VideoEncoderHandle"/>, <see cref="VideoDecoderHandle"/>,
/// <see cref="AudioEncoderHandle"/> and <see cref="AudioDecoderHandle"/> for the work each one
/// actually accepts.
/// </summary>
/// <remarks>
/// A codec runs asynchronously: submitted work is queued and the results arrive on the callback the
/// handle was created with, not from the submitting call. <see cref="GetQueueSize"/> is how a
/// producer knows to slow down, and <see cref="Flush"/> is how it knows everything has come out.
/// </remarks>
public abstract class WebCodecsHandle : IAsyncDisposable
{
    private protected readonly IJSRuntime Js;
    private protected readonly Guid HandleId;
    private IDisposable? _callbackRef;
    private bool _disposed;

    private protected WebCodecsHandle(IJSRuntime js, Guid id)
    {
        Js = js;
        HandleId = id;
    }

    /// <summary>The internal codec id.</summary>
    public Guid Id => HandleId;

    private protected void TrackCallbackRef(IDisposable callbackRef) => _callbackRef = callbackRef;

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoEncoder/state">state</see>:
    /// whether the codec is configured and accepting work.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async ValueTask<CodecState> GetState() => ToState(await Js.Invoke<string>("BitButil.webCodecs.state", HandleId));

    /// <summary>
    /// How much work is still queued -
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoEncoder/encodeQueueSize">encodeQueueSize</see>
    /// or its decode counterpart.
    /// </summary>
    /// <remarks>
    /// The backpressure signal: a producer that keeps submitting while this climbs will run the tab
    /// out of memory, since every queued frame holds its pixels. Capture the next frame when it
    /// drops instead.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<int> GetQueueSize() => Js.Invoke<int>("BitButil.webCodecs.queueSize", HandleId);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoEncoder/flush">flush()</see>:
    /// completes once everything queued has been emitted on the output callback.
    /// </summary>
    /// <returns>False when the codec was reset, closed or errored while the flush was pending.</returns>
    /// <remarks>
    /// What to await before muxing or uploading: an encoder can hold several frames back to make
    /// better decisions, so the last chunk of a recording usually appears only here.
    /// </remarks>
    public ValueTask<bool> Flush() => Js.Invoke<bool>("BitButil.webCodecs.flush", HandleId);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoEncoder/reset">reset()</see>:
    /// throws away the queue and the configuration, leaving the codec
    /// <see cref="CodecState.Unconfigured"/>.
    /// </summary>
    /// <remarks>
    /// What a seek calls: the pending work belongs to the old position and its output would be wrong.
    /// Configure again before submitting anything else.
    /// </remarks>
    public ValueTask Reset() => Js.InvokeVoid("BitButil.webCodecs.reset", HandleId);

    /// <summary>
    /// Closes the codec and releases its resources - a hardware encoder among them. Calling it again
    /// does nothing, and a closed codec cannot be reconfigured.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await Js.InvokeVoid("BitButil.webCodecs.close", HandleId); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _callbackRef?.Dispose();
            _callbackRef = null;
        }
        GC.SuppressFinalize(this);
    }

    private protected static CodecState ToState(string? raw) => raw switch
    {
        "configured" => CodecState.Configured,
        "unconfigured" => CodecState.Unconfigured,
        _ => CodecState.Closed
    };

    private protected static EncodedChunkType ToChunkType(string? raw)
        => raw == "delta" ? EncodedChunkType.Delta : EncodedChunkType.Key;
}
