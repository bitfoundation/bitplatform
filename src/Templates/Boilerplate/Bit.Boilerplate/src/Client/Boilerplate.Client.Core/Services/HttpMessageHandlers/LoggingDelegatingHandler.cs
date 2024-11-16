using System.Diagnostics;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

internal class LoggingDelegatingHandler(ILogger<HttpClient> logger, HttpMessageHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending HTTP request {Method} {Uri}", request.Method, request.RequestUri);

        var stopwatch = Stopwatch.StartNew();
        int? responseStatusCode = null;
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (RestException exp)
        {
            responseStatusCode = (int)exp.StatusCode;
            throw;
        }
        catch (HttpRequestException exp)
        {
            responseStatusCode = (int?)exp.StatusCode;
            throw;
        }
        finally
        {
            logger.LogInformation("Received HTTP response for {Uri} after {Duration}ms - {StatusCode}",
                request.RequestUri,
                stopwatch.ElapsedMilliseconds,
                responseStatusCode);
        }
    }
}
