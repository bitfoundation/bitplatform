using Microsoft.AspNetCore.SignalR.Client;
using Boilerplate.Client.Core.Services.HttpMessageHandlers;

namespace Boilerplate.Client.Core.Services;

public class SignalrInfinitiesRetryPolicy : IRetryPolicy
{
    private IEnumerator<TimeSpan>? delays;

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        if (retryContext.PreviousRetryCount == 0)
        {
            delays?.Dispose();
            delays = RetryDelegatingHandler.GetDelaySequence(scaleFirstTry: TimeSpan.FromSeconds(3)).GetEnumerator();
        }

        delays!.MoveNext();
        return delays.Current;
    }
}
