namespace Bit.Bmotion;

/// <summary>
/// The rules for handing a transition to an animation the browser plays on its own - the compositor
/// offload (WAAPI) and prerendered CSS animations. Both paths go through here so they agree on which
/// transitions the browser can play as the engine would, and on the curve it plays them with.
/// </summary>
internal static class BmotionBrowserTiming
{
    /// <summary>The resolution a tween's curve is sampled at for a <c>linear()</c> easing.</summary>
    internal const int TweenSampleCount = 33;

    // Springs are simulated once per distinct curve: a page full of identical springs (a staggered
    // list, say) would otherwise run the same simulation for every element and property.
    private const int MaxCachedSprings = 64;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<SpringKey, (double[] Samples, double DurationMs)?> _springs = new();

    private readonly record struct SpringKey(
        double Stiffness, double Damping, double Mass, double Velocity, double RestSpeed, double RestDelta,
        double Duration, double? Bounce, double? VisualDuration);

    /// <summary>
    /// Whether a browser-played animation can reproduce <paramref name="config"/>'s timing. Says
    /// nothing about the values being animated - each caller checks those against what it can drive.
    /// </summary>
    internal static bool CanPlay(BmotionTransitionConfig config)
    {
        if (config.Type == BmotionTransitionType.Inertia) return false;
        if (config.RepeatDelay > 0) return false;
        // Reverse plays forwards once and backwards after that - two different animations.
        if (config.RepeatType == BmRepeatType.Reverse && (config.IsInfiniteRepeat || config.Repeat > 0)) return false;
        // Browser keyframes sit at evenly spaced offsets.
        if (config.Times is { } times && !IsEvenlySpaced(times)) return false;
        if (!double.IsFinite(config.Delay)) return false;
        // A spring with initial velocity produces a per-distance curve that a normalized sample
        // table can't represent.
        if (config.Type == BmotionTransitionType.Spring) return config.Velocity == 0;
        return config.Duration > 0 && double.IsFinite(config.Duration);
    }

    /// <summary>
    /// The transition's eased progress over one iteration, sampled for a <c>linear()</c> easing, and
    /// how long that iteration lasts. <paramref name="ease"/> overrides the tween curve (a keyframe
    /// segment's ease, say). Null when a spring doesn't settle within 20 s. The samples are shared:
    /// don't modify them.
    /// </summary>
    internal static (double[] Samples, double DurationMs)? SampleProgress(BmotionTransitionConfig config, Func<double, double>? ease = null)
    {
        if (config.Type == BmotionTransitionType.Spring) return SampleSpringProgress(config);
        return (SampleTweenProgress(ease ?? BmEaseFunctions.Get(config)), config.Duration * 1000);
    }

    /// <summary>
    /// The CSS easing to play <paramref name="samples"/> with, or null when the browser can't play
    /// it faithfully. <paramref name="preferKeyword"/> picks an exact CSS keyword / cubic-bezier over
    /// <c>linear()</c> where one exists (shorter, and it needs no <c>linear()</c> support).
    /// </summary>
    internal static string? ChooseEasing(BmotionTransitionConfig config, double[] samples, bool linearSupported, bool preferKeyword)
    {
        if (config.Type == BmotionTransitionType.Spring) return linearSupported ? BuildLinearEasing(samples) : null;

        bool hasKeyword = config.Eases is null && BmEaseFunctions.HasFaithfulCssEasing(config);
        if (hasKeyword && (preferKeyword || !linearSupported)) return BmEaseFunctions.ToCssString(config);
        // Elastic/bounce have no CSS curve: without linear() the browser can't play them.
        return linearSupported ? BuildLinearEasing(samples) : null;
    }

    internal static double[] SampleTweenProgress(Func<double, double> ease)
    {
        var samples = new double[TweenSampleCount];
        for (int i = 0; i < TweenSampleCount; i++)
            samples[i] = ease((double)i / (TweenSampleCount - 1));
        return samples;
    }

    /// <summary>
    /// Simulates the spring on a normalized 0→1 distance (valid for any distance when initial
    /// velocity is zero - the ODE is linear) and returns the eased-progress samples plus the
    /// settling duration. Null when the spring doesn't settle within 20 s.
    /// </summary>
    internal static (double[] Samples, double DurationMs)? SampleSpringProgress(BmotionTransitionConfig config)
    {
        var key = new SpringKey(config.Stiffness, config.Damping, config.Mass, config.Velocity, config.RestSpeed,
            config.RestDelta, config.Duration, config.Bounce, config.VisualDuration);
        if (_springs.TryGetValue(key, out var cached)) return cached;

        var sampled = SimulateSpring(config);
        if (_springs.Count >= MaxCachedSprings) _springs.Clear();
        _springs[key] = sampled;
        return sampled;
    }

    private static (double[] Samples, double DurationMs)? SimulateSpring(BmotionTransitionConfig config)
    {
        var sampling = config.Clone();
        sampling.Delay = 0;
        sampling.Repeat = 0;
        sampling.RepeatInfinite = false;
        sampling.RepeatDelay = 0;

        double value = 0;
        BmotionSpringDriver driver;
        try { driver = new BmotionSpringDriver(0, 1, sampling, v => value = v); }
        catch { return null; }

        const double stepMs = 1000.0 / 60;
        const int maxSamples = 20 * 60;
        var samples = new List<double>(128);
        double t = 0;
        bool done = false;
        for (int i = 0; i < maxSamples && !done; i++)
        {
            done = driver.Tick(t);
            samples.Add(value);
            t += stepMs;
        }
        if (!done || samples.Count < 2) return null;
        samples[^1] = 1; // land exactly on the target
        return (samples.ToArray(), (samples.Count - 1) * stepMs);
    }

    internal static string BuildLinearEasing(double[] samples)
    {
        var sb = new System.Text.StringBuilder("linear(");
        for (int i = 0; i < samples.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(Math.Round(samples[i], 4).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        return sb.Append(')').ToString();
    }

    private static bool IsEvenlySpaced(double[] times)
    {
        if (times.Length < 2) return true;
        for (int i = 0; i < times.Length; i++)
            if (Math.Abs(times[i] - ((double)i / (times.Length - 1))) > 1e-6) return false;
        return true;
    }
}
