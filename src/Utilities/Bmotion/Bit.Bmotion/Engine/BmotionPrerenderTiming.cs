namespace Bit.Bmotion;

/// <summary>One CSS animation's timing, in the units the <c>animation</c> shorthand takes.</summary>
internal sealed record BmotionPrerenderTiming(double DurationMs, string Easing, double DelayMs, int Iterations, bool Alternate)
{
    /// <summary>Whether the animation repeats forever (<see cref="Iterations"/> is negative).</summary>
    public bool IsInfinite => Iterations < 0;

    /// <summary>When a finite animation has finished, measured from the moment it was created.</summary>
    public double EndMs => DelayMs + (DurationMs * Iterations);
}
