namespace Bit.Websites.Careers.Client.Services.HttpMessageHandlers;

public class RetryDelegatingHandler(ExceptionDelegatingHandler handler)
    : DelegatingHandler(handler)
{

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var delays = GetDelays(scaleFirstTry: TimeSpan.FromSeconds(3), maxRetries: 3).ToArray();

        Exception? lastExp = null;

        foreach (var delay in delays)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception exp) when (exp is not KnownException)
            {
                lastExp = exp;
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastExp!;
    }

    private static IEnumerable<TimeSpan> GetDelays(TimeSpan scaleFirstTry, int maxRetries)
    {
        TimeSpan maxValue = TimeSpan.MaxValue;
        var maxTimeSpanDouble = maxValue.Ticks - 1_000.0;
        var i = 0;
        var targetTicksFirstDelay = scaleFirstTry.Ticks;
        var num = 0.0;
        for (; i < maxRetries; i++)
        {
            var num2 = i + Random.Shared.NextDouble();
            var next = Math.Pow(2.0, num2) * Math.Tanh(Math.Sqrt(4.0 * num2));
            var num3 = next - num;
            yield return TimeSpan.FromTicks((long)Math.Min(num3 * 0.7_142_857_142_857_143 * targetTicksFirstDelay, maxTimeSpanDouble));
            num = next;
        }
    }
}
