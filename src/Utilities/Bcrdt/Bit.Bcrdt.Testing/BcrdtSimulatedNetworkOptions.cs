namespace Bit.Bcrdt;

/// <summary>How badly the simulated network misbehaves.</summary>
public sealed class BcrdtSimulatedNetworkOptions
{
    /// <summary>The random seed. Every run with the same seed produces the same schedule.</summary>
    public int Seed { get; init; } = 1;

    /// <summary>The chance that a message is silently discarded.</summary>
    public double DropProbability { get; init; }

    /// <summary>The chance that a message is delivered twice.</summary>
    public double DuplicateProbability { get; init; }

    /// <summary>The largest number of delivery steps a message may be delayed by.</summary>
    /// <remarks>
    /// Non-zero latency is what produces reordering: messages sent in one order arrive in another,
    /// which is precisely the condition the convergence proof has to hold under. With zero latency,
    /// messages are delivered in exactly the order they were sent.
    /// </remarks>
    public int MaxLatencySteps { get; init; }

    /// <summary>How far the simulated clock advances with each delivery step.</summary>
    /// <remarks>
    /// Drives presence expiry deterministically: a peer that falls silent disappears from the
    /// others' participant lists after a fixed number of steps, not after a wall-clock interval.
    /// </remarks>
    public TimeSpan StepDuration { get; init; } = TimeSpan.FromMilliseconds(100);
}
