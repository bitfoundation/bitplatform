using Microsoft.AspNetCore.SignalR.Client;

namespace Boilerplate.Client.Core.Services;

public class SignalRInfiniteRetryPolicy : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        return TimeSpan.FromSeconds(1); // It's already handled by HttpMessageHandlers/RetryDelegatingHandler during negotiate http request.
    }
}
