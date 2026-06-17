using System.Runtime.CompilerServices;

namespace Bit.Bmotion;
/// <summary>
/// Controls for an in-flight programmatic animation started by
/// <see cref="BmotionAnimateService.AnimateAsync(string,BmotionAnimationProps,BmotionTransitionConfig?)"/>.
/// <para>The object is directly awaitable - <c>await controls</c> waits for the animation to complete.</para>
/// <para>
/// <see cref="Stop"/> freezes the animation at its current (intermediate) values; <see cref="Complete"/>
/// jumps it to its target (end) values. Both release the engine refcount immediately, so they also
/// safely stop infinite-repeat animations (whose completion task never resolves on its own).
/// </para>
/// </summary>
public sealed class BmotionAnimationControls
{
    private readonly IReadOnlyList<string> _elementIds;
    private readonly BmotionAnimationEngine _engine;
    private readonly Task _completion;
    private readonly Action _release;
    private int _released;

    internal BmotionAnimationControls(
        IReadOnlyList<string> elementIds, BmotionAnimationEngine engine, Task completion, Action release)
    {
        _elementIds = elementIds;
        _engine = engine;
        _completion = completion;
        _release = release;
    }

    // Release the engine refcount exactly once - whether the animation finishes naturally, is
    // stopped, or is completed early. Without this, an infinite-repeat animation (which never
    // finishes) would pin its elements in the engine forever.
    private void ReleaseOnce()
    {
        if (System.Threading.Interlocked.Exchange(ref _released, 1) == 0)
            _release();
    }

    /// <summary>
    /// Immediately cancel all running animations on the target elements.
    /// Elements snap to their current (intermediate) positions.
    /// </summary>
    public void Stop()
    {
        // Atomically claim ownership: only the caller that flips _released from 0→1 runs the engine
        // side effects. Once released (e.g. a natural finish already settled the completion), the
        // target elements may be owned by newer animations - skip so we don't disturb them.
        if (System.Threading.Interlocked.CompareExchange(ref _released, 1, 0) != 0) return;
        try
        {
            foreach (var id in _elementIds)
                _engine.Stop(id, null);
        }
        finally
        {
            _release();
        }
    }

    /// <summary>
    /// Cancel all running animations and snap elements to their target (end) values.
    /// </summary>
    public void Complete()
    {
        // See Stop(): atomically claim ownership so engine side effects run exactly once and never
        // after a concurrent settlement has handed the elements to newer animations.
        if (System.Threading.Interlocked.CompareExchange(ref _released, 1, 0) != 0) return;
        try
        {
            foreach (var id in _elementIds)
                _engine.Complete(id);
        }
        finally
        {
            _release();
        }
    }

    /// <summary>A <see cref="Task"/> that resolves when all animations finish naturally.</summary>
    public Task WhenCompleteAsync() => _completion;

    /// <summary>Makes <see cref="BmotionAnimationControls"/> directly awaitable.</summary>
    public TaskAwaiter GetAwaiter() => _completion.GetAwaiter();

    // Invoked by the owning service once the completion task settles, so a natural finish also
    // releases the refcount (idempotent with Stop/Complete).
    internal void OnCompletionSettled() => ReleaseOnce();
}
