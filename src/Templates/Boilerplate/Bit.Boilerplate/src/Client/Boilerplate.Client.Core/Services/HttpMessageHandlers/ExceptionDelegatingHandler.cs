//+:cnd:noEmit
using System.Net;
using System.Net.Sockets;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public partial class ExceptionDelegatingHandler(PubSubService pubSubService,
                                                IStringLocalizer<AppStrings> localizer,
                                                JsonSerializerOptions jsonSerializerOptions,
                                                AbsoluteServerAddressProvider absoluteServerAddress,
                                                HttpMessageHandler handler) : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var logScopeData = (Dictionary<string, object?>)request.Options.GetValueOrDefault(RequestOptionNames.LogScopeData)!;

        bool serverCommunicationSuccess = false;
        var isInternalRequest = request.RequestUri!.ToString().StartsWith(absoluteServerAddress, StringComparison.InvariantCultureIgnoreCase);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
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

            serverCommunicationSuccess = true;

            if (isInternalRequest && /* The following exception handling mechanism applies exclusively to responses from our own server. */
                response.IsSuccessStatusCode is false &&
                response.Content.Headers.ContentType?.MediaType?.Contains("application/json", StringComparison.InvariantCultureIgnoreCase) is true)
            {
                var problemDetails = (await response.Content.ReadFromJsonAsync(jsonSerializerOptions.GetTypeInfo<AppProblemDetails>(), cancellationToken))!;

                Type exceptionType = typeof(KnownException).Assembly.GetType(problemDetails.Type!) ?? typeof(UnknownException);

                var args = new List<object?> { typeof(KnownException).IsAssignableFrom(exceptionType) ? new LocalizedString(problemDetails.Key!.ToString()!, problemDetails.Title!) : (object?)problemDetails.Title! };

                Exception exp = exceptionType == typeof(ResourceValidationException)
                                    ? new ResourceValidationException(problemDetails.Title!, problemDetails.Payload)
                                    : (Exception)Activator.CreateInstance(exceptionType, args.ToArray())!;

                throw exp;
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedException(localizer[nameof(AppStrings.YouNeedToSignIn)]);
            }
            if (response.StatusCode is HttpStatusCode.Forbidden)
            {
                throw new ForbiddenException(localizer[nameof(AppStrings.ForbiddenException)]);
            }

            response.EnsureSuccessStatusCode();

            request.Options.Set(new(RequestOptionNames.LogLevel), LogLevel.Information);

            return response;
        }
        catch (Exception exp) when (
               (exp is HttpRequestException && serverCommunicationSuccess is false)
            || (exp is TaskCanceledException tcExp && tcExp.InnerException is TimeoutException)
            || (exp.InnerException is SocketException sockExp && sockExp.SocketErrorCode is SocketError.HostNotFound)
            || (exp is HttpRequestException { StatusCode: HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout or HttpStatusCode.ServiceUnavailable }))
        {
            serverCommunicationSuccess = false; // Let's treat the server communication as failed if an exception is caught here.
            throw new ServerConnectionException(localizer[nameof(AppStrings.ServerConnectionException)], exp);
        }
        finally
        {
            if (isInternalRequest)
            {
                pubSubService.Publish(ClientPubSubMessages.IS_ONLINE_CHANGED, serverCommunicationSuccess);
            }
        }
    }
}
