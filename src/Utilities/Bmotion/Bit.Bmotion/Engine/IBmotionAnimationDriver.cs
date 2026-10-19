namespace Bit.Bmotion;
/// <summary>
/// Single animation driver interface.
/// Each driver owns the callback that applies the animated value to
/// <see cref="BmotionElementAnimationState"/> state dictionaries.
/// Returns <c>true</c> from <see cref="Tick"/> when the animation is complete.
/// </summary>
internal interface IBmotionAnimationDriver
{
    /// <summary>
    /// Advance the animation to <paramref name="timestamp"/> (milliseconds, matching <c>performance.now()</c>).
    /// Calls the apply-callback with the current value.
    /// Returns <c>true</c> when the animation has finished and may be removed.
    /// </summary>
    bool Tick(double timestamp);

    /// <summary>Cancel the animation, freezing the value at its current (intermediate) position.</summary>
    void Cancel();

    /// <summary>Finish immediately by applying the animation's final target value.</summary>
    void Complete();
}
