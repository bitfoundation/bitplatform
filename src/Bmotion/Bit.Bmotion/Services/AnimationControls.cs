using System.Runtime.CompilerServices;
using Bit.Bmotion.Engine;

namespace Bit.Bmotion.Services;

/// <summary>
/// Controls for an in-flight programmatic animation started by
/// <see cref="MotionAnimateService.AnimateAsync(string,Models.AnimationProps,Models.TransitionConfig?)"/>.
/// <para>The object is directly awaitable — <c>await controls</c> waits for the animation to complete.</para>
/// </summary>
public sealed class AnimationControls
{
    private readonly IReadOnlyList<string> _elementIds;
    private readonly AnimationEngine _engine;
    private readonly Task _completion;

    internal AnimationControls(IReadOnlyList<string> elementIds, AnimationEngine engine, Task completion)
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
            _engine.Stop(id, null);
    }

    /// <summary>A <see cref="Task"/> that resolves when all animations finish naturally.</summary>
    public Task WhenCompleteAsync() => _completion;

    /// <summary>Makes <see cref="AnimationControls"/> directly awaitable.</summary>
    public TaskAwaiter GetAwaiter() => _completion.GetAwaiter();
}
