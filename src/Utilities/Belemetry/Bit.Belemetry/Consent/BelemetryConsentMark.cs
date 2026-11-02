using System.Diagnostics;

namespace Bit.Belemetry;

/// <summary>
/// The consent a batch or request was built under: the features its records belong to, and how many times they had
/// been revoked by then. Once one of them is revoked again it is withdrawn - neither sent, retried nor stored - as it
/// may hold the revoked feature's records.
/// </summary>
internal readonly struct BelemetryConsentMark(BelemetryRevocations revocations, BelemetryFeatures features, int count)
{
    /// <summary>The features of its records.</summary>
    public BelemetryFeatures Features => features;

    /// <summary>Whether one of <see cref="Features"/> was revoked since it was built.</summary>
    public bool IsWithdrawn => revocations.Count(features) != count;

    /// <summary>
    /// Waits for <paramref name="delay"/>, or less: until it is withdrawn. A revoke of other features wakes it up too,
    /// and it then waits for the rest of the delay.
    /// </summary>
    public async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();
        while (true)
        {
            // The token before the check: a revoke after the check cancels it.
            var revoke = revocations.NextRevoke;
            var left = delay - Stopwatch.GetElapsedTime(start);
            if (IsWithdrawn || left <= TimeSpan.Zero) return;

            using var wait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, revoke);
            try
            {
                await Task.Delay(left, wait.Token);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested is false)
            {
                // A revoke: withdrawn, or of other features.
            }
        }
    }
}
