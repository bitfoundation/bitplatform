using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Handle to a live <c>MediaStream</c> obtained via <see cref="MediaDevices.GetUserMedia"/>.
/// Stop the stream by disposing the handle - every track is stopped and the stream is dropped.
/// </summary>
public sealed class MediaStreamHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private bool _disposed;

    internal MediaStreamHandle(IJSRuntime js, Guid id) { _js = js; _id = id; }

    /// <summary>The internal stream id used to track this stream (see <see cref="AttachTo"/>).</summary>
    public Guid Id => _id;

    /// <summary>Attaches this stream to a <c>&lt;video&gt;</c> or <c>&lt;audio&gt;</c> element's <c>srcObject</c>.</summary>
    public ValueTask AttachTo(ElementReference videoOrAudioElement)
        => _js.InvokeVoid("BitButil.mediaDevices.attach", _id, videoOrAudioElement);

    /// <summary>Pauses every track without dropping the stream.</summary>
    public ValueTask SetEnabled(bool enabled)
        => _js.InvokeVoid("BitButil.mediaDevices.setEnabled", _id, enabled);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.mediaDevices.stop", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
