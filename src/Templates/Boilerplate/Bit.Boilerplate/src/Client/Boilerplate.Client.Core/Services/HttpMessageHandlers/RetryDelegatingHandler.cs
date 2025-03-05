namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public partial class RetryDelegatingHandler(HttpMessageHandler handler)
    : DelegatingHandler(handler)
{

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var logScopeData = (Dictionary<string, object?>)request.Options.GetValueOrDefault(RequestOptionNames.LogScopeData)!;
        var delays = GetDelaySequence(scaleFirstTry: TimeSpan.FromSeconds(3)).Take(3).ToArray();

        Exception? lastExp = null;

        int retryCount = 0;
        foreach (var delay in delays)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception exp) when (exp is not KnownException || exp is ServerConnectionException) // If the exception is either unknown or a server connection issue, let's retry once more.
            {
                if (request.HasNoRetryPolicyAttribute() || AppEnvironment.IsDev())
                    throw;
                retryCount++;
                logScopeData["RetryCount"] = retryCount;
                lastExp = exp;
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastExp!;
    }

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
