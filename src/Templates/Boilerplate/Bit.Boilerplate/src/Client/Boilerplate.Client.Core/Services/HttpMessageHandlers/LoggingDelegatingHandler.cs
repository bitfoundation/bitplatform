using System.Diagnostics;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

internal class LoggingDelegatingHandler(ILogger<HttpClient> logger, HttpMessageHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending HTTP request {Method} {Uri}", request.Method, request.RequestUri);

        var stopwatch = Stopwatch.StartNew();

        HttpResponseMessage? response = null;

        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            logger.LogInformation("Received HTTP response for {Uri} after {Duration}ms - {StatusCode}",
                request.RequestUri,
                stopwatch.ElapsedMilliseconds,
                response is null ? -1 : (int)response.StatusCode);
        }

        return response;
    }
}
