namespace Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

public partial class RetryDelegatingHandler(HttpMessageHandler handler)
    : DelegatingHandler(handler)
{

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var logScopeData = (Dictionary<string, object?>)request.Options.GetValueOrDefault(RequestOptionNames.LogScopeData)!;
        const int maxRetries = 3;
        var delays = GetDelaySequence(scaleFirstTry: TimeSpan.FromSeconds(3)).Take(maxRetries - 1).ToArray();

        Exception? lastExp = null;

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

                if (exp is KnownException)
                    throw; // There's no benefit in retrying known exceptions, for example when the Category's name is excepted to be unique, retying won't help.

                lastExp = exp;

                // Only wait if there are retries left
                if (attempt < maxRetries - 1)
                {
                    logScopeData["RetryCount"] = attempt + 1;
                    await Task.Delay(delays[attempt], cancellationToken);
                }
            }
        }

        throw lastExp!;
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
