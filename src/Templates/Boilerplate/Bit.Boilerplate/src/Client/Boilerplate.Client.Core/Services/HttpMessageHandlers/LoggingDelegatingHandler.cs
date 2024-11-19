using System.Diagnostics;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

internal class LoggingDelegatingHandler(ILogger<HttpClient> logger, HttpMessageHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending HTTP request {Method} {Uri}", request.Method, request.RequestUri);
        request.Options.Set(new("LogLevel"), LogLevel.Warning);
        request.Options.Set(new("LogScopeData"), new Dictionary<string, object?>());

        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            var logLevel = (LogLevel)request.Options.GetValueOrDefault("LogLevel")!;
            var logScopeData = (Dictionary<string, object?>)request.Options.GetValueOrDefault("LogScopeData")!;

            using var scope = logger.BeginScope(logScopeData);
            logger.Log(logLevel, "Received HTTP response for {Uri} after {Duration}ms",
                request.RequestUri,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
