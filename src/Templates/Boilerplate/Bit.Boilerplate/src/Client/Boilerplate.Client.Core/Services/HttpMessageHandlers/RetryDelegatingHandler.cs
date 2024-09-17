﻿using System.Reflection;
using Boilerplate.Shared.Controllers;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public partial class RetryDelegatingHandler(HttpMessageHandler handler)
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
            catch (Exception exp) when (exp is not KnownException || exp is ServerConnectionException) // If the exception is either unknown or a server connection issue, let's retry once more.
            {
                if (HasNoRetryPolicy(request))
                    throw;

                lastExp = exp;
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastExp!;
    }

    /// <summary>
    /// <see cref="NoRetryPolicyAttribute"/>
    /// </summary>
    private static bool HasNoRetryPolicy(HttpRequestMessage request)
    {
        if (request.Options.TryGetValue(new(RequestOptionNames.IControllerType), out Type? controllerType) is false)
            return false;

        var parameterTypes = ((Dictionary<string, Type>)request.Options.GetValueOrDefault(RequestOptionNames.ActionParametersInfo)!).Select(p => p.Value).ToArray();
        var method = controllerType!.GetMethod((string)request.Options.GetValueOrDefault(RequestOptionNames.ActionName)!, parameterTypes)!;
        return method.GetCustomAttribute<NoRetryPolicyAttribute>() is not null;
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
