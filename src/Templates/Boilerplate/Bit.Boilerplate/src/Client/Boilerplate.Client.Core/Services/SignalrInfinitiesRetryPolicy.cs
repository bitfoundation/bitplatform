using Microsoft.AspNetCore.SignalR.Client;

namespace Boilerplate.Client.Core.Services;

public class SignalrInfinitiesRetryPolicy : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        return TimeSpan.FromMilliseconds(1); // Retry policy is handling in HttpMessageHandlers/RetryDelegatingHandler.
    }
}
