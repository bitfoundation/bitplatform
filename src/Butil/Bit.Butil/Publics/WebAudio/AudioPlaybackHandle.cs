using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Handle to a one-shot Web Audio playback (<see cref="WebAudio.PlayBuffer"/> /
/// <see cref="WebAudio.PlayTone"/>). Dispose to stop early.
/// </summary>
public sealed class AudioPlaybackHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private bool _disposed;

    internal AudioPlaybackHandle(IJSRuntime js, Guid id) { _js = js; _id = id; }

    /// <summary>The internal playback id.</summary>
    public Guid Id => _id;

    /// <summary>Stops playback immediately.</summary>
    public ValueTask Stop() => _js.InvokeVoid("BitButil.webAudio.stop", _id);

    /// <summary>Sets per-source gain in [0, 1].</summary>
    public ValueTask SetGain(double value) => _js.InvokeVoid("BitButil.webAudio.setGain", _id, value);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.webAudio.stop", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
