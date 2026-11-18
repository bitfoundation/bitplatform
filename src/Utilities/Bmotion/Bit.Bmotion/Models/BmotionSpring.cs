namespace Bit.Bmotion;

/// <summary>Physics-based spring driven by stiffness, damping and mass - or, more intuitively,
/// by <see cref="Bounce"/> and <see cref="Duration"/>.</summary>
public sealed class BmotionSpring : BmotionTransition
{
    /// <summary>Spring stiffness. Higher = snappier. Default: 100.</summary>
    public double Stiffness { get; set; } = 100;

    /// <summary>Damping coefficient. Higher = less oscillation. Default: 10.</summary>
    public double Damping { get; set; } = 10;

    /// <summary>Virtual mass. Higher = slower acceleration. Default: 1.</summary>
    public double Mass { get; set; } = 1;

    /// <summary>Initial velocity (units/s). Default: 0.</summary>
    public double Velocity { get; set; }

    /// <summary>Minimum speed (units/s) considered at rest. Default: 0.01.</summary>
    public double RestSpeed { get; set; } = 0.01;

    /// <summary>Minimum distance from target considered at rest. Default: 0.01.</summary>
    public double RestDelta { get; set; } = 0.01;

    /// <summary>
    /// Bounciness (0 = no bounce, 1 = very bouncy). When set (or when <see cref="Duration"/>
    /// is set), stiffness and damping are derived automatically.
    /// </summary>
    public double? Bounce { get; set; }

    /// <summary>
    /// The visual time (seconds) the spring appears to take to reach its target
    /// (bounce may continue after). Combines with <see cref="Bounce"/> (default 0.25)
    /// to configure the spring without physics parameters.
    /// </summary>
    public double? Duration { get; set; }

    /// <summary>
    /// Measures how this spring moves from <paramref name="from"/> to <paramref name="to"/> - settling time,
    /// overshoot, peak velocity, damping ratio, oscillation period and a 60 Hz trace - by running the engine's own
    /// spring driver on a virtual clock, so the numbers are what the animation does on screen. Its
    /// <see cref="Velocity"/> is the initial velocity; delays and repeats are left out.
    /// <code>
    /// var a = BmotionSprings.Bouncy().Analyze();
    /// // a.SettlingTime ≈ 0.83 s, a.Overshoot ≈ 0.11 (11 % past the target), a.TargetCrossings == 3
    /// </code>
    /// Use it to match a CSS transition or a sequence to a spring, to choose between springs, or to chart one.
    /// </summary>
    /// <param name="from">Where the motion starts. Default 0.</param>
    /// <param name="to">Where it ends. Default 1, so <see cref="BmotionSpringAnalysis.PeakVelocity"/> reads as distances per second.</param>
    /// <exception cref="ArgumentException">
    /// A value or the spring's configuration isn't finite, or its stiffness isn't positive or its damping is negative.
    /// </exception>
    public BmotionSpringAnalysis Analyze(double from = 0, double to = 1) => BmotionSpringAnalyzer.Analyze(this, from, to);

    private protected override BmotionTransitionConfig CreateConfig()
    {
        var c = new BmotionTransitionConfig
        {
            Type = BmotionTransitionType.Spring,
            Stiffness = Stiffness,
            Damping = Damping,
            Mass = Mass,
            Velocity = Velocity,
            RestSpeed = RestSpeed,
            RestDelta = RestDelta,
        };
        // A duration-based spring derives stiffness/damping from bounce + visual duration.
        // Setting either of the intuitive parameters opts into that model.
        if (Duration.HasValue || Bounce.HasValue)
        {
            c.Bounce = Bounce ?? 0.25;
            c.VisualDuration = Duration ?? 0.5;
        }
        return c;
    }

    private protected override BmotionTransition CloneTransition()
    {
        var copy = new BmotionSpring
        {
            Stiffness = Stiffness,
            Damping = Damping,
            Mass = Mass,
            Velocity = Velocity,
            RestSpeed = RestSpeed,
            RestDelta = RestDelta,
            Bounce = Bounce,
            Duration = Duration,
        };
        CopyBaseTo(copy);
        return copy;
    }

    internal override bool ValueEquals(BmotionTransition other)
        => other is BmotionSpring s
           && Stiffness == s.Stiffness && Damping == s.Damping && Mass == s.Mass
           && Velocity == s.Velocity && RestSpeed == s.RestSpeed && RestDelta == s.RestDelta
           && Bounce == s.Bounce && Duration == s.Duration
           && base.ValueEquals(other);
}
