//+:cnd:noEmit
using System.Web;
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
        var logScopeData = (Dictionary<string, object?>)request.Options.GetValueOrDefault(RequestOptionNames.LogScopeData)!;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (AppPlatform.IsBrowser is false)
            {
                logScopeData["HttpVersion"] = response.Version;
            }
            if (response.Headers.TryGetValues("Request-Id", out var requestId))
            {
                logScopeData["RequestId"] = requestId.First();
            }
            //#if (cloudflare == true)
            if (response.Headers.TryGetValues("Cf-Cache-Status", out var cfCacheStatus)) // Cloudflare cache status
            {
                logScopeData["Cf-Cache-Status"] = cfCacheStatus.First();
            }
            //#endif
            if (response.Headers.TryGetValues("Age", out var age)) // ASP.NET Core Output Caching
            {
                logScopeData["Age"] = age.First();
            }
            if (response.Headers.TryGetValues("App-Cache-Response", out var appCacheResponse))
            {
                logScopeData["App-Cache-Response"] = appCacheResponse.First();
            }
            logScopeData["HttpStatusCode"] = response.StatusCode;
            return response;
        }
        finally
        {
            var logLevel = (LogLevel)request.Options.GetValueOrDefault(RequestOptionNames.LogLevel)!;

            using var scope = logger.BeginScope(logScopeData);
            logger.Log(logLevel, "Received HTTP response for {Uri} after {Duration}ms",
                HttpUtility.UrlDecode(request.RequestUri!.ToString()),
                stopwatch.ElapsedMilliseconds.ToString("N0"));
        }
    }
}
