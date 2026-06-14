using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Handle to an in-flight Web Animation. Always dispose (or cancel) so the animation
/// is removed from the engine - long-running animations otherwise sit on the element
/// indefinitely with <see cref="AnimationOptions.Fill"/> set.
/// </summary>
public sealed class AnimationHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private bool _disposed;

    internal AnimationHandle(IJSRuntime js, Guid id)
    {
        _js = js;
        _id = id;
    }

    /// <summary>Plays a paused animation.</summary>
    public ValueTask Play() => _js.InvokeVoid("BitButil.animation.play", _id);

    /// <summary>Pauses the animation at its current time.</summary>
    public ValueTask Pause() => _js.InvokeVoid("BitButil.animation.pause", _id);

    /// <summary>Reverses playback direction.</summary>
    public ValueTask Reverse() => _js.InvokeVoid("BitButil.animation.reverse", _id);

    /// <summary>Cancels and removes the animation immediately.</summary>
    public ValueTask Cancel() => _js.InvokeVoid("BitButil.animation.cancel", _id);

    /// <summary>Jumps to the end of the animation, applying <see cref="AnimationOptions.Fill"/>.</summary>
    public ValueTask Finish() => _js.InvokeVoid("BitButil.animation.finish", _id);

    /// <summary>Awaits the animation's <c>finished</c> Promise.</summary>
    public ValueTask WhenFinished() => _js.InvokeVoid("BitButil.animation.whenFinished", _id);

    /// <summary>Sets the playback rate (1 = normal speed; -1 = reverse at normal speed).</summary>
    public ValueTask SetPlaybackRate(double rate) => _js.InvokeVoid("BitButil.animation.setPlaybackRate", _id, rate);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.animation.cancel", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
