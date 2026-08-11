namespace Boilerplate.Client.Core.Infrastructure.Services;

public class SignalRInfiniteRetryPolicy : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        // The negotiate http request is backed off by HttpMessageHandlers/RetryDelegatingHandler - but only in released
        // Browser and Blazor Hybrid builds, because that handler rethrows immediately in Development and on any other
        // host. So on Blazor Server, and while developing, one second is the whole delay.
        return TimeSpan.FromSeconds(1);
    }
}
