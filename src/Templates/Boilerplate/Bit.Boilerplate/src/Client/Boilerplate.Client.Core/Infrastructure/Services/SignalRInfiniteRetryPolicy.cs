namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// Retries forever. An attempt that took seconds was already backed off by RetryDelegatingHandler on its negotiate request;
/// one that failed fast was not (`SkipNegotiation` true, Development, Blazor Server), so the wait grows here instead.
/// </summary>
public class SignalRInfiniteRetryPolicy : IRetryPolicy
{
    private int fastFailures;
    private TimeSpan previousElapsedTime;
    private TimeSpan previousDelay;

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        var attemptDuration = retryContext.ElapsedTime - previousElapsedTime - previousDelay;

        fastFailures = retryContext.PreviousRetryCount is 0 || attemptDuration >= TimeSpan.FromSeconds(2) ? 0 : fastFailures + 1;

        previousElapsedTime = retryContext.ElapsedTime;

        // 1, 2, 4... up to 30 seconds, with jitter so the clients of a server that went down don't all come back at once.
        previousDelay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, fastFailures), 30) * Random.Shared.Next(80, 121) / 100);

        return previousDelay;
    }
}
