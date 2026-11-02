using System.Numerics;

namespace Bit.Belemetry;

/// <summary>
/// Counts the user's revokes, per feature. A batch or request built before a revoke of a feature its records belong to
/// may hold that feature's records, so it is neither sent, retried nor stored after it (<see cref="BelemetryConsentMark"/>).
/// Counted per feature so that a revoke of another one withdraws nothing: a request of error logs survives the user
/// turning off interactions, and revoking <see cref="BelemetryFeatures.Persistence"/> or
/// <see cref="BelemetryFeatures.Sessions"/>, which no record belongs to, withdraws nothing in flight.
/// </summary>
internal sealed class BelemetryRevocations
{
    private readonly int[] _counts = new int[32];
    private CancellationTokenSource _next = new();

    /// <summary>Cancelled at the next revoke: a request waiting for its retry wakes up to see whether it was withdrawn.</summary>
    public CancellationToken NextRevoke => Volatile.Read(ref _next).Token;

    public void Revoke(BelemetryFeatures features)
    {
        for (var bits = (uint)features; bits != 0; bits &= bits - 1)
        {
            Interlocked.Increment(ref _counts[BitOperations.TrailingZeroCount(bits)]);
        }

        // After the counts: a waiter that took the old token is woken up, and one that takes the new token already
        // sees the counts. The old source is not disposed - a waiter may still link its token - and holds no timer.
        Interlocked.Exchange(ref _next, new CancellationTokenSource()).Cancel();
    }

    /// <summary>The mark of a batch or request built now from records of <paramref name="features"/>.</summary>
    public BelemetryConsentMark Mark(BelemetryFeatures features) => new(this, features, Count(features));

    /// <summary>How many times any of <paramref name="features"/> was revoked so far.</summary>
    public int Count(BelemetryFeatures features)
    {
        var count = 0;
        for (var bits = (uint)features; bits != 0; bits &= bits - 1)
        {
            count += Volatile.Read(ref _counts[BitOperations.TrailingZeroCount(bits)]);
        }
        return count;
    }
}
