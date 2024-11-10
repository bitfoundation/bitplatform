using Microsoft.AspNetCore.SignalR.Client;

namespace Boilerplate.Client.Core.Services;

public class SignalrInfinitiesRetryPolicy : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        return TimeSpan.FromSeconds(5);
    }
}
