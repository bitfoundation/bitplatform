namespace Bit.Bmotion;

/// <summary>
/// How a spring moves from one value to another - measured by running the engine's own spring driver, so it is what
/// the animation does on screen (the frame loop and the browser play the same curve), not a textbook approximation.
/// Produced by <see cref="BmotionSpring.Analyze"/>.
/// </summary>
public sealed record BmotionSpringAnalysis
{
    /// <summary>The stiffness the spring runs with - derived from <see cref="BmotionSpring.Bounce"/>/<see cref="BmotionSpring.Duration"/> when those are set.</summary>
    public required double Stiffness { get; init; }

    /// <summary>The damping it runs with - derived like <see cref="Stiffness"/>.</summary>
    public required double Damping { get; init; }

    /// <summary>The mass it runs with.</summary>
    public required double Mass { get; init; }

    /// <summary>Damping ratio ζ = damping / (2·√(stiffness·mass)): 1 is critical damping, below it the spring bounces.</summary>
    public required double DampingRatio { get; init; }

    /// <summary>Whether it overshoots and oscillates, settles at once, or creeps in.</summary>
    public required BmotionSpringDamping DampingKind { get; init; }

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
