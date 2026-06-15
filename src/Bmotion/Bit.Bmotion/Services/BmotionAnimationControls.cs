using System.Runtime.CompilerServices;

namespace Bit.Bmotion;
/// <summary>
/// Controls for an in-flight programmatic animation started by
/// <see cref="BmotionAnimateService.AnimateAsync(string,BmotionAnimationProps,BmotionTransitionConfig?)"/>.
/// <para>The object is directly awaitable - <c>await controls</c> waits for the animation to complete.</para>
/// </summary>
public sealed class BmotionAnimationControls
{
    private readonly IReadOnlyList<string> _elementIds;
    private readonly BmotionAnimationEngine _engine;
    private readonly Task _completion;

    internal BmotionAnimationControls(IReadOnlyList<string> elementIds, BmotionAnimationEngine engine, Task completion)
    {
        _elementIds = elementIds;
        _engine = engine;
        _completion = completion;
    }

    /// <summary>
    /// Immediately cancel all running animations on the target elements.
    /// Elements snap to their current (intermediate) positions.
    /// </summary>
    public void Stop()
    {
        foreach (var id in _elementIds)
            _engine.Stop(id, null);
    }

    /// <summary>
    /// Cancel all running animations and snap elements to their target (end) values.
    /// </summary>
    public void Complete()
    {
        foreach (var id in _elementIds)
            _engine.Complete(id);
    }

    /// <summary>A <see cref="Task"/> that resolves when all animations finish naturally.</summary>
    public Task WhenCompleteAsync() => _completion;

    /// <summary>Makes <see cref="BmotionAnimationControls"/> directly awaitable.</summary>
    public TaskAwaiter GetAwaiter() => _completion.GetAwaiter();
}
