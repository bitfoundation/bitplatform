namespace Bit.Blow;

/// <summary>
/// How often to try a node again after it fails. Attempts wait <see cref="Delay"/>, multiplied by
/// <see cref="BackoffFactor"/> after each one, up to <see cref="MaxDelay"/>.
/// </summary>
public sealed record BlowRetryPolicy
{
    /// <summary>No retries: one attempt.</summary>
    public static readonly BlowRetryPolicy None = new() { MaxAttempts = 1 };

    /// <summary>The number of attempts in all, the first included. At least 1.</summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>The wait before the second attempt.</summary>
    public TimeSpan Delay { get; init; } = TimeSpan.FromMilliseconds(200);

    /// <summary>What each wait is multiplied by for the next: 2 doubles it (exponential backoff), 1 keeps it constant.</summary>
    public double BackoffFactor { get; init; } = 2;

    /// <summary>The longest wait between two attempts.</summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether a failure is worth another attempt. Null retries every failure but a <see cref="BlowNodeException"/>
    /// whose <see cref="BlowNodeException.IsTransient"/> is false. A timeout counts as a failure.
    /// </summary>
    public Func<Exception, bool>? ShouldRetry { get; init; }

    /// <summary>The wait before the given attempt (2 for the first retry).</summary>
    public TimeSpan DelayBefore(int attempt)
    {
        if (attempt <= 1 || Delay <= TimeSpan.Zero) return TimeSpan.Zero;
        var ms = Delay.TotalMilliseconds * Math.Pow(Math.Max(BackoffFactor, 1), attempt - 2);
        var max = MaxDelay.TotalMilliseconds;
        return TimeSpan.FromMilliseconds(double.IsFinite(ms) ? Math.Min(ms, max) : max);
    }

    internal bool Allows(Exception error)
        => ShouldRetry?.Invoke(error) ?? error is not BlowNodeException { IsTransient: false };
}
