namespace Bit.Bmotion;

/// <summary>Runs a spring's own driver on a virtual clock and measures it.</summary>
internal static class BmotionSpringAnalyzer
{
    internal const int SampleRate = 60;
    // The frame loop and the recorder step the driver once per 60 Hz frame, and a spring integrated in bigger steps
    // moves (slightly) differently: stepping it the same way is what makes the numbers the ones on screen.
    private const double StepMs = 1000.0 / SampleRate;
    private const double LimitMs = 60_000;

    public static BmotionSpringAnalysis Analyze(BmotionSpring spring, double from, double to)
    {
        if (!double.IsFinite(from) || !double.IsFinite(to))
            throw new ArgumentException("A spring is analysed between two finite values.", nameof(from));

        // The driver checks the rest of the configuration and falls back from a mass <= 0, but takes an infinite one -
        // a spring that never moves, so never rests - and a NaN mass is no configuration to measure either.
        if (!double.IsFinite(spring.Mass))
            throw new ArgumentException("A spring is analysed with a finite mass.", nameof(spring));

        // One pass, starting at once: repeats and delays aren't the spring's motion.
        var config = spring.ToConfig();
        config.Delay = 0;
        config.Repeat = 0;
        config.RepeatInfinite = false;
        config.RepeatDelay = 0;

        double value = from;
        var driver = new BmotionSpringDriver(from, to, config, v => value = v);

        // The physics the driver resolved (bounce/duration derivation, mass fallback) - the spring that actually runs.
        double k = driver.Stiffness, d = driver.Damping, m = driver.Mass;
        // A spring that could never settle runs with the driver's defaults instead: it is not the one asked about.
        if (!driver.Settles)
            throw new ArgumentException("A spring is analysed with a positive stiffness and a non-negative damping.", nameof(spring));

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
        var kind = Math.Abs(zeta - 1) <= 1e-3 ? BmotionSpringDamping.CriticallyDamped
            : zeta < 1 ? BmotionSpringDamping.Underdamped : BmotionSpringDamping.Overdamped;

        return new BmotionSpringAnalysis
        {
            Stiffness = k,
            Damping = d,
            Mass = m,
            DampingRatio = zeta,
            DampingKind = kind,
            NaturalFrequency = omega0,
            OscillationPeriod = kind == BmotionSpringDamping.Underdamped ? 2 * Math.PI / (omega0 * Math.Sqrt(1 - (zeta * zeta))) : null,
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
