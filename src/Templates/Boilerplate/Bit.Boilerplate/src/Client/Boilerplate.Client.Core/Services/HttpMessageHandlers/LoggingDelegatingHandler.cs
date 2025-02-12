﻿using System.Web;
using System.Diagnostics;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

internal class LoggingDelegatingHandler(ILogger<HttpClient> logger, HttpMessageHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending HTTP request {Method} {Uri}", request.Method, HttpUtility.UrlDecode(request.RequestUri?.ToString()));
        request.Options.Set(new(RequestOptionNames.LogLevel), LogLevel.Warning);
        request.Options.Set(new(RequestOptionNames.LogScopeData), new Dictionary<string, object?>());

        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            var logLevel = (LogLevel)request.Options.GetValueOrDefault(RequestOptionNames.LogLevel)!;
            var logScopeData = (Dictionary<string, object?>)request.Options.GetValueOrDefault(RequestOptionNames.LogScopeData)!;

            using var scope = logger.BeginScope(logScopeData);
            logger.Log(logLevel, "Received HTTP response for {Uri} after {Duration}ms",
                HttpUtility.UrlDecode(request.RequestUri!.ToString()),
                stopwatch.ElapsedMilliseconds.ToString("N0"));
        }
    }
}
