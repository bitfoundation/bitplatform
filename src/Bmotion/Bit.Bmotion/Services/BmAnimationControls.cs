using System.Runtime.CompilerServices;

namespace Bit.Bmotion;
/// <summary>
/// Controls for an in-flight programmatic animation started by
/// <see cref="BmotionAnimateService"/>.
/// <para>The object is directly awaitable - <c>await controls</c> waits for the animation to complete.</para>
/// <para>
/// <see cref="Stop"/> freezes the animation at its current (intermediate) values; <see cref="Complete"/>
/// jumps it to its target (end) values. Both release the engine refcount immediately, so they also
/// safely stop infinite-repeat animations (whose completion task never resolves on its own).
/// </para>
/// </summary>
public sealed class BmAnimationControls
{
    private readonly IReadOnlyList<string> _elementIds;
    private readonly BmotionAnimationEngine _engine;
    private readonly Task _completion;
    private readonly Action _release;
    private int _released;
    // Serializes the release/claim decision with the engine calls it guards: without it,
    // SetSpeed could observe _released == 0, lose the CPU, and then touch elements a concurrent
    // Stop/Complete/natural settlement has already released (and newer animations now own).
    private readonly object _sync = new();

    internal BmAnimationControls(
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
        lock (_sync)
        {
            if (System.Threading.Interlocked.Exchange(ref _released, 1) == 0)
                _release();
        }
    }

    /// <summary>Pauses the animation in place (equivalent to <c>Speed = 0</c>).</summary>
    public void Pause() => SetSpeed(0);

    /// <summary>Resumes a paused animation at normal speed.</summary>
    public void Play() => SetSpeed(1);

    /// <summary>
    /// Sets the playback rate: 1 = realtime, 0 = paused, 2 = twice as fast.
    /// No-op once the animation has settled (stopped, completed, or finished naturally) -
    /// the elements may already be owned by newer animations.
    /// </summary>
    public void SetSpeed(double speed)
    {
        lock (_sync)
        {
            if (System.Threading.Volatile.Read(ref _released) != 0) return;
            foreach (var id in _elementIds)
                _engine.SetPlaybackRate(id, speed);
        }
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
        lock (_sync)
        {
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
    }

    /// <summary>
    /// Cancel all running animations and snap elements to their target (end) values.
    /// </summary>
    public void Complete()
    {
        // See Stop(): atomically claim ownership so engine side effects run exactly once and never
        // after a concurrent settlement has handed the elements to newer animations.
        lock (_sync)
        {
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
    }

    /// <summary>A <see cref="Task"/> that resolves when all animations finish naturally.</summary>
    public Task WhenCompleteAsync() => _completion;

    /// <summary>Makes <see cref="BmAnimationControls"/> directly awaitable.</summary>
    public TaskAwaiter GetAwaiter() => _completion.GetAwaiter();

    // Invoked by the owning service once the completion task settles, so a natural finish also
    // releases the refcount (idempotent with Stop/Complete).
    internal void OnCompletionSettled() => ReleaseOnce();
}
