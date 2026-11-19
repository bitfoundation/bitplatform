namespace Bit.Bmotion;

/// <summary>
/// What a programmatic animation's controls need to report and move its time: how long it lasts, how to move it in
/// place, how to play it again from part-way in, and how to take hold of its elements again once it has ended.
/// </summary>
internal sealed class BmotionPlayback
{
    /// <summary>The animation's length in seconds (see <see cref="BmotionAnimationControls.Duration"/>).</summary>
    public double Duration { get; init; }

    /// <summary>Moves the current run to this many seconds into it in place; false when it can't.</summary>
    public Func<double, bool> TrySeekInPlace { get; init; } = static _ => false;

    /// <summary>Plays the animation again from this many seconds in; returns the new run's completion.</summary>
    public required Func<double, Task> PlayFrom { get; init; }

    /// <summary>Registers the elements with the engine again, after the animation released them.</summary>
    public required Action Rearm { get; init; }
}
