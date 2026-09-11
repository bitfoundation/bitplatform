using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A delegated ink trail started by <see cref="Bit.Butil.Ink.StartTrail"/>. Dispose it to stop
/// painting - when the tool changes, when drawing ends, or when the component goes away.
/// </summary>
public sealed class InkTrailHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private bool _disposed;

    internal InkTrailHandle(IJSRuntime js, Guid id) { _js = js; _id = id; }

    /// <summary>The internal trail id.</summary>
    public Guid Id => _id;

    /// <summary>
    /// Changes the trail's colour and width from the next point onwards - what a colour or brush-size
    /// picker calls, without stopping and restarting the trail.
    /// </summary>
    /// <param name="color">The new colour, as any CSS colour string.</param>
    /// <param name="diameter">The new width in pixels. Pass 0 to keep the current one.</param>
    /// <returns>False when the trail is already gone.</returns>
    public ValueTask<bool> SetStyle(string color, double diameter = 0)
        => _js.Invoke<bool>("BitButil.ink.setStyle", _id, color, diameter);

    /// <summary>Stops the trail and detaches its pointer listener. Calling it again does nothing.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.ink.stop", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
