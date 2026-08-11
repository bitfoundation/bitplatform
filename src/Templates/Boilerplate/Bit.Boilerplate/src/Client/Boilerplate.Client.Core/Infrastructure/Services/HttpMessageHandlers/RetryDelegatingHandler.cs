using System.Runtime.ExceptionServices;

namespace Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

public partial class RetryDelegatingHandler(HttpMessageHandler handler)
    : DelegatingHandler(handler)
{

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var logScopeData = (Dictionary<string, object?>)request.Options.GetValueOrDefault(RequestOptionNames.LogScopeData)!;
        const int maxRetries = 3;
        var delays = GetDelaySequence(scaleFirstTry: TimeSpan.FromSeconds(3)).Take(maxRetries - 1).ToArray();

        ExceptionDispatchInfo? lastExp = null;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception exp)
            {
                if (request.HasNoRetryPolicyAttribute())
                    throw;

                if (AppEnvironment.IsDevelopment())
                    throw;

                if (AppPlatform.IsBlazorHybrid is false && AppPlatform.IsBrowser is false)
                    throw; // Disable retry-policy during pre-rendering and Blazor Server.

                // There's no benefit in retrying known exceptions, for example when the Category's name is expected
                // to be unique, retrying won't help.
                // KnownException also includes TooManyRequestsException: Trying to retry a request that was throttled is not going to help, the server will still throttle the request.
                if (exp is KnownException and not TransientException)
                    throw;

                // Captured rather than stored, because `throw lastExp` at the end would reset the stack trace to that
                // line - discarding where the failure actually came from, on the one path whose whole purpose is to
                // report an unrecoverable network failure. The retry loop is disabled in Development, so the mangled
                // trace only ever existed in telemetry from released clients.
                lastExp = ExceptionDispatchInfo.Capture(exp);

                // Only wait if there are retries left
                if (attempt < maxRetries - 1)
                {
                    logScopeData["RetryCount"] = attempt + 1;
                    await Task.Delay(delays[attempt], cancellationToken);
                }
            }
        }

        lastExp!.Throw();
        throw null; // Unreachable: Throw() above never returns, but the compiler cannot know that.
    }

    /// <summary>
    /// Generates an infinite sequence of exponentially increasing delays with jitter for retry attempts.
    /// </summary>
    private static IEnumerable<TimeSpan> GetDelaySequence(TimeSpan scaleFirstTry)
    {
        TimeSpan maxValue = TimeSpan.MaxValue;
        var maxTimeSpanDouble = maxValue.Ticks - 1_000.0;
        var i = 0;
        var targetTicksFirstDelay = scaleFirstTry.Ticks;
        var num = 0.0;
        for (; i < int.MaxValue; i++)
        {
            var num2 = i + Random.Shared.NextDouble();
            var next = Math.Pow(2.0, num2) * Math.Tanh(Math.Sqrt(4.0 * num2));
            var num3 = next - num;
            yield return TimeSpan.FromTicks((long)Math.Min(num3 * 0.7_142_857_142_857_143 * targetTicksFirstDelay, maxTimeSpanDouble));
            num = next;
        }
    }
}
