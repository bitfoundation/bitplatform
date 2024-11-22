using Microsoft.AspNetCore.SignalR.Client;

namespace Boilerplate.Client.Core.Services;

public class SignalRInfinitiesRetryPolicy : IRetryPolicy
{
    private static TimeSpan[] delays = new double[] { 1, 3, 5, 10, 15, 20, 30 }
        .Select(TimeSpan.FromSeconds)
        .ToArray();

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        var index = retryContext.PreviousRetryCount;

        if (index < delays.Length)
        {
            return delays[index];
        }

        return TimeSpan.FromSeconds(30);
    }
}
