namespace Bit.Bmotion;

/// <summary>
/// Plays the time a physics driver (<see cref="BmotionSpringDriver"/>, <see cref="BmotionInertiaDriver"/>) skips when it
/// starts part-way in - a negative delay, which is also how a seek plays an animation again from a point. The skipped
/// time is played frame by frame, so the motion lands where it would have had it played: one big step would integrate
/// it far more coarsely than frames do, and could step over a bound crossed meanwhile.
/// <para>
/// At most the last <see cref="MaxMs"/> of it are played. Played whole, a huge delay keeps the thread busy for as long
/// as it names - forever for one so large that adding a frame no longer moves the clock - and on Blazor Server that
/// thread is the server's. A motion that settles does so long before the cap and lands exactly as before; only one
/// that never settles (an undamped spring, one that repeats forever) lands where it is <see cref="MaxMs"/> in rather
/// than where it would be after the whole delay.
/// </para>
/// </summary>
internal static class BmotionFastForward
{
    /// <summary>The frame the skipped time is played in: 60 Hz, as the browser draws.</summary>
    internal const double FrameMs = 1000.0 / 60;

    /// <summary>The most skipped time that is played: ten minutes.</summary>
    internal const double MaxMs = 10 * 60 * 1000;

    /// <summary>
    /// The most frames that are played. <see cref="MaxMs"/> bounds them already; counting them as well keeps the loop
    /// finite on a clock so large that adding a frame no longer moves it.
    /// </summary>
    internal const int MaxFrames = (int)(MaxMs / FrameMs) + 2;

    /// <summary>
    /// Plays the <paramref name="skippedMs"/> before <paramref name="timestamp"/> through <paramref name="tick"/>, a
    /// frame at a time - at most the last <see cref="MaxMs"/> of it. True when the motion finished meanwhile.
    /// </summary>
    internal static bool Play(double skippedMs, double timestamp, Func<double, bool> tick)
    {
        double t = timestamp - Math.Min(skippedMs, MaxMs);
        for (int frame = 0; frame < MaxFrames && t < timestamp; frame++, t += FrameMs)
            if (tick(t)) return true;
        return false;
    }
}
