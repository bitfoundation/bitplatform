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

    /// <summary>
    /// Writes the animation's current computed values into the element's inline style, so the end
    /// state outlives the animation.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Animation/commitStyles">https://developer.mozilla.org/en-US/docs/Web/API/Animation/commitStyles</see>
    /// </summary>
    /// <returns>False when the runtime has no <c>commitStyles</c>, or the effect can't be committed
    /// (no target, or the element isn't rendered).</returns>
    /// <remarks>
    /// The usual pairing is <c>await WhenFinished(); await CommitStyles(); await Cancel();</c> - the
    /// alternative, leaving a filling animation in place forever, keeps the element under the
    /// animation's control and beats any later style change.
    /// </remarks>
    public ValueTask<bool> CommitStyles() => _js.Invoke<bool>("BitButil.animation.commitStyles", _id);

    /// <summary>
    /// Opts the animation out of automatic removal.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Animation/persist">https://developer.mozilla.org/en-US/docs/Web/API/Animation/persist</see>
    /// </summary>
    /// <returns>False when the runtime has no <c>persist</c>.</returns>
    /// <remarks>
    /// The browser discards a finished filling animation once another one supersedes it, and its
    /// visual effect goes with it. Persisting keeps it - but it also keeps it fighting every later
    /// animation of the same property, so <see cref="CommitStyles"/> is usually the better answer.
    /// </remarks>
    public ValueTask<bool> Persist() => _js.Invoke<bool>("BitButil.animation.persist", _id);

    /// <summary>Cancels the animation and releases the browser-side handle. Calling it again does nothing.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.animation.cancel", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
