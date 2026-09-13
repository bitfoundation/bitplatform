using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A view transition in flight, returned by <see cref="ViewTransition.Start(Func{Task}, string[])"/>.
/// </summary>
/// <remarks>
/// Awaiting is optional - the animation runs whether or not anyone watches it. The two waits are
/// there for the cases that need them: <see cref="WaitForReady"/> to start a companion animation
/// in lockstep with the transition's own, and <see cref="WaitForFinished"/> to do something once
/// the page has settled.
/// </remarks>
public sealed class ViewTransitionHandle
{
    private readonly IJSRuntime _js;
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal ViewTransitionHandle(IJSRuntime js, Guid id)
    {
        _js = js;
        Id = id;
    }

    /// <summary>The internal transition id.</summary>
    public Guid Id { get; }

    /// <summary>
    /// True when the transition was skipped - either by <see cref="Skip"/> or by the browser,
    /// which skips rather than animates when the update takes too long or the page is hidden.
    /// </summary>
    /// <remarks>
    /// A skipped transition still applies the DOM update and still finishes; only the animation is
    /// dropped.
    /// </remarks>
    public bool WasSkipped { get; private set; }

    /// <summary>
    /// Completes when the pseudo-element tree exists and the animation is about to run - the
    /// moment to start anything that has to move in step with it.
    /// </summary>
    /// <remarks>
    /// Also completes (rather than faulting) when the transition is skipped, in which case
    /// <see cref="WasSkipped"/> is true and there is nothing to animate alongside.
    /// </remarks>
    public Task WaitForReady() => _ready.Task;

    /// <summary>
    /// Completes when the animation has ended and the page is in its new state - including when the
    /// transition was skipped.
    /// </summary>
    public Task WaitForFinished() => _finished.Task;

    /// <summary>
    /// Jumps to the end state immediately, dropping the animation. The DOM update still applies and
    /// <see cref="WaitForFinished"/> still completes.
    /// </summary>
    /// <remarks>
    /// Useful when the user acts again before the previous transition finished - animating to a
    /// state they have already left is worse than not animating at all.
    /// </remarks>
    public ValueTask Skip() => _js.InvokeVoid("BitButil.viewTransition.skip", Id);

    // Driven by ViewTransition's [JSInvokable] phase callback. TrySetResult rather than SetResult
    // throughout: a skip resolves 'ready' and then 'finished' arrives too, and a phase that fires
    // twice must not throw out of an interop dispatch.
    internal void Advance(string phase, string message)
    {
        switch (phase)
        {
            case "ready":
                _ready.TrySetResult();
                break;

            case "skipped":
                WasSkipped = true;
                _ready.TrySetResult();
                break;

            case "finished":
                _ready.TrySetResult();
                _finished.TrySetResult();
                break;

            case "failed":
                // The update callback threw. Surfacing it on 'finished' is what lets a caller that
                // awaited notice, while a caller that didn't await is unaffected.
                _ready.TrySetResult();
                _finished.TrySetException(new InvalidOperationException(
                    string.IsNullOrEmpty(message) ? "The view transition failed." : message));
                break;
        }
    }
}
