namespace Bit.Bmotion;

/// <summary>How a spring's damping shapes its motion.</summary>
public enum BmSpringDamping
{
    /// <summary>Damping ratio below 1: it overshoots the target and oscillates around it before it rests.</summary>
    Underdamped,

    /// <summary>Damping ratio of 1 (within 0.1 %): the fastest approach that doesn't overshoot.</summary>
    CriticallyDamped,

    /// <summary>Damping ratio above 1: it creeps onto the target, slower than critical damping and without overshoot.</summary>
    Overdamped,
}

/// <summary>
/// How a spring moves from one value to another - measured by running the engine's own spring driver, so it is what
/// the animation does on screen (the frame loop and the browser play the same curve), not a textbook approximation.
/// Produced by <see cref="BmSpring.Analyze"/>.
/// </summary>
public sealed record BmSpringAnalysis
{
    /// <summary>The stiffness the spring runs with - derived from <see cref="BmSpring.Bounce"/>/<see cref="BmSpring.Duration"/> when those are set.</summary>
    public required double Stiffness { get; init; }

    /// <summary>The damping it runs with - derived like <see cref="Stiffness"/>.</summary>
    public required double Damping { get; init; }

    /// <summary>The mass it runs with.</summary>
    public required double Mass { get; init; }

    /// <summary>Damping ratio ζ = damping / (2·√(stiffness·mass)): 1 is critical damping, below it the spring bounces.</summary>
    public required double DampingRatio { get; init; }

    /// <summary>Whether it overshoots and oscillates, settles at once, or creeps in.</summary>
    public required BmSpringDamping DampingKind { get; init; }

    /// <summary>Undamped natural frequency ω₀ = √(stiffness/mass), in radians per second.</summary>
    public required double NaturalFrequency { get; init; }

    /// <summary>
    /// Seconds per oscillation for an underdamped spring (2π / (ω₀·√(1-ζ²))); <c>null</c> when it doesn't oscillate.
    /// </summary>
    public double? OscillationPeriod { get; init; }

    /// <summary>
    /// Seconds until the spring comes to rest - the moment the animation ends and its completion resolves (the
    /// rest thresholds scale with the distance, as they do on screen).
    /// </summary>
    public required double SettlingTime { get; init; }

    /// <summary>
    /// Whether it came to rest within the simulated minute. <c>false</c> for an undamped or almost undamped spring;
    /// <see cref="SettlingTime"/> is then the time simulated.
    /// </summary>
    public required bool Settled { get; init; }

    /// <summary>
    /// How far past the target it travels at most, as a fraction of the distance (0.1 = 10 %); 0 when it never passes
    /// the target, or when the two values are equal.
    /// </summary>
    public required double Overshoot { get; init; }

    /// <summary>Seconds until the furthest point of the overshoot; 0 when there is none.</summary>
    public required double PeakTime { get; init; }

    /// <summary>The highest speed it reaches, in units per second (absolute).</summary>
    public required double PeakVelocity { get; init; }

    /// <summary>How many times it passes the target before it rests (0 without overshoot).</summary>
    public required int TargetCrossings { get; init; }

    /// <summary>Where it ends: the target.</summary>
    public required double FinalValue { get; init; }

    /// <summary>
    /// Its value <see cref="SampleRate"/> times a second from the start until it rests - for a chart, or to compare
    /// two springs frame by frame.
    /// </summary>
    public required IReadOnlyList<double> Samples { get; init; }

    /// <summary>Samples per second in <see cref="Samples"/> (60).</summary>
    public int SampleRate => BmotionSpringAnalyzer.SampleRate;
}

/// <summary>Runs a spring's own driver on a virtual clock and measures it.</summary>
internal static class BmotionSpringAnalyzer
{
    internal const int SampleRate = 60;
    // The frame loop and the recorder step the driver once per 60 Hz frame, and a spring integrated in bigger steps
    // moves (slightly) differently: stepping it the same way is what makes the numbers the ones on screen.
    private const double StepMs = 1000.0 / SampleRate;
    private const double LimitMs = 60_000;

    public static BmSpringAnalysis Analyze(BmSpring spring, double from, double to)
    {
        if (!double.IsFinite(from) || !double.IsFinite(to))
            throw new ArgumentException("A spring is analysed between two finite values.", nameof(from));

        // One pass, starting at once: repeats and delays aren't the spring's motion.
        var config = spring.ToConfig();
        config.Delay = 0;
        config.Repeat = 0;
        config.RepeatInfinite = false;
        config.RepeatDelay = 0;

        double k = config.Stiffness, d = config.Damping;
        double m = config.Mass > 0 ? config.Mass : 1;
        if (config.Bounce.HasValue)
            (k, d) = BmotionTransitionConfig.SpringFromBounce(config.VisualDuration ?? config.Duration, config.Bounce.Value, m);

        double value = from;
        var driver = new BmotionSpringDriver(from, to, config, v => value = v);

        double distance = to - from;
        double direction = Math.Sign(distance);
        var samples = new List<double> { from };
        double nextSampleMs = 1000.0 / SampleRate;
        double previous = from, peakVelocity = Math.Abs(config.Velocity);
        double overshoot = 0, peakTime = 0;
        int crossings = 0;
        double side = Math.Sign(from - to);
        bool settled = false;
        double t = 0;

        driver.Tick(0); // seeds the clock
        while (t < LimitMs)
        {
            t += StepMs;
            settled = driver.Tick(t);

            peakVelocity = Math.Max(peakVelocity, Math.Abs(value - previous) / (StepMs / 1000));
            previous = value;

            double past = (value - to) * direction;
            if (direction != 0 && past > overshoot)
            {
                overshoot = past;
                peakTime = t / 1000;
            }
            double nowSide = Math.Sign(value - to);
            if (nowSide != 0 && side != 0 && nowSide != side && !settled) crossings++;
            if (nowSide != 0) side = nowSide;

            while (t >= nextSampleMs)
            {
                samples.Add(value);
                nextSampleMs += 1000.0 / SampleRate;
            }
            if (settled) break;
        }
        if (settled && samples[^1] != to) samples.Add(to);

        double zeta = d / (2 * Math.Sqrt(k * m));
        double omega0 = Math.Sqrt(k / m);
        var kind = Math.Abs(zeta - 1) <= 1e-3 ? BmSpringDamping.CriticallyDamped
            : zeta < 1 ? BmSpringDamping.Underdamped : BmSpringDamping.Overdamped;

        return new BmSpringAnalysis
        {
            Stiffness = k,
            Damping = d,
            Mass = m,
            DampingRatio = zeta,
            DampingKind = kind,
            NaturalFrequency = omega0,
            OscillationPeriod = kind == BmSpringDamping.Underdamped ? 2 * Math.PI / (omega0 * Math.Sqrt(1 - (zeta * zeta))) : null,
            SettlingTime = t / 1000,
            Settled = settled,
            Overshoot = distance == 0 ? 0 : overshoot / Math.Abs(distance),
            PeakTime = overshoot > 0 ? peakTime : 0,
            PeakVelocity = peakVelocity,
            TargetCrossings = crossings,
            FinalValue = to,
            Samples = samples,
        };
    }
}
