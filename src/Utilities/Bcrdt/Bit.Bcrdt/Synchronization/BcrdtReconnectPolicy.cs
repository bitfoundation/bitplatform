namespace Bit.Bcrdt;

/// <summary>
/// How long to wait between reconnection attempts.
/// </summary>
/// <remarks>
/// <para>
/// Uses full jitter - a delay drawn uniformly from zero to the exponential ceiling - rather than a
/// fixed backoff. When a server restarts, every client it was serving tries to reconnect at once;
/// a deterministic schedule means they all retry in lockstep and knock it over again. Randomizing
/// the whole interval spreads them out, which is the difference between a server recovering and a
/// server being held down by its own clients.
/// </para>
/// <para>
/// A policy is safe to share between providers and threads: one instance commonly serves every
/// document an application opens, and those documents reconnect concurrently after an outage.
/// </para>
/// </remarks>
public sealed class BcrdtReconnectPolicy
{
    /// <summary>The starting delay, doubled on each successive failure.</summary>
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>The ceiling the exponential growth stops at.</summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>How many times to try before giving up. Defaults to forever.</summary>
    public int MaxAttempts { get; init; } = int.MaxValue;

    /// <summary>Whether reconnection is attempted at all.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>A policy that never reconnects, for tests and one-shot syncs.</summary>
    public static BcrdtReconnectPolicy None { get; } = new() { Enabled = false, MaxAttempts = 0 };

    /// <summary>The delay to wait before attempt number <paramref name="attempt"/>.</summary>
    public TimeSpan NextDelay(int attempt)
    {
        if (attempt <= 0) return TimeSpan.Zero;

        // Cap the exponent before it is applied, so a long outage cannot overflow the multiplication.
        int exponent = Math.Min(attempt - 1, 20);
        double ceiling = Math.Min(BaseDelay.TotalMilliseconds * Math.Pow(2, exponent), MaxDelay.TotalMilliseconds);

        // Random.Shared rather than a per-policy instance: System.Random is not thread-safe, and one
        // policy is shared by every provider that reconnects after the same outage.
        return TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * Math.Max(ceiling, 0));
    }
}
