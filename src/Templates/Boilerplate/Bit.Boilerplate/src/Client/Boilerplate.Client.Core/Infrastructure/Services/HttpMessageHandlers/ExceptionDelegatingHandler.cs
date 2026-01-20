//+:cnd:noEmit
using System.Net;
using System.Net.Sockets;

namespace Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

public partial class ExceptionDelegatingHandler(PubSubService pubSubService,
                                                IStringLocalizer<AppStrings> localizer,
                                                JsonSerializerOptions jsonSerializerOptions,
                                                HttpMessageHandler handler) : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var logScopeData = (Dictionary<string, object?>)request.Options.GetValueOrDefault(RequestOptionNames.LogScopeData)!;

        bool serverCommunicationSuccess = false;
        var isInternalRequest = request.HasExternalApiAttribute() is false;

        string? requestIdValue = null;

        try
        {
            try
            {
                var response = await base.SendAsync(request, cancellationToken);

                if (response.Headers.TryGetValues("Request-Id", out var requestId))
                {
                    requestIdValue = requestId.First();
                    logScopeData["RequestId"] = requestIdValue;
                }

                serverCommunicationSuccess = true;

                if (isInternalRequest && /* The following exception handling mechanism applies exclusively to responses from our own server. */
                    response.IsSuccessStatusCode is false &&
                    string.IsNullOrEmpty(requestIdValue) is false &&
                    response.Content.Headers.ContentType?.MediaType?.Contains("application/json", StringComparison.InvariantCultureIgnoreCase) is true)
                {
                    var problemDetails = (await response.Content.ReadFromJsonAsync(jsonSerializerOptions.GetTypeInfo<AppProblemDetails>(), cancellationToken))!;

                    throw problemDetails;
                }

                if (response.StatusCode is HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedException(localizer[nameof(AppStrings.YouNeedToSignIn)]);
                }
                if (response.StatusCode is HttpStatusCode.Forbidden)
                {
                    throw new ForbiddenException(localizer[nameof(AppStrings.ForbiddenException)]);
                }
                if (response.StatusCode is HttpStatusCode.TooManyRequests)
                {
                    throw new TooManyRequestsException(localizer[nameof(AppStrings.TooManyRequestsException)]);
                }

                response.EnsureSuccessStatusCode();

                request.Options.Set(new(RequestOptionNames.LogLevel), LogLevel.Information);

                return response;
            }
            catch (ClientNotSupportedException)
            {
                pubSubService.Publish(ClientAppMessages.FORCE_UPDATE, persistent: true);
                throw;
            }
            catch (Exception exp) when (IsServerConnectionException(exp) || (exp is HttpRequestException && serverCommunicationSuccess is false))
            {
                serverCommunicationSuccess = false; // Let's treat the server communication as failed if an exception is caught here.
                throw new ServerConnectionException(localizer[nameof(AppStrings.ServerConnectionException)], exp);
            }
            finally
            {
                if (isInternalRequest)
                {
                    pubSubService.Publish(ClientAppMessages.IS_ONLINE_CHANGED, serverCommunicationSuccess);
                }
            }
        }
        catch (Exception exp)
        {
            exp.WithData("RequestId", requestIdValue ?? "?"); // Connect the exception to its corresponding request id, if one exists.
            throw;
        }
    }

    private bool IsServerConnectionException(Exception exp)
    {
        return (exp is TimeoutException)
             || (exp is WebException webExp && webExp.WithData("Status", webExp.Status).Status is WebExceptionStatus.ConnectFailure)
             || (exp.InnerException is not null && IsServerConnectionException(exp.InnerException))
             || (exp is HttpIOException httpIOExp && httpIOExp.WithData("HttpRequestError", httpIOExp.HttpRequestError).HttpRequestError is not HttpRequestError.UserAuthenticationError)
             || (exp is AggregateException aggExp && aggExp.InnerExceptions.Any(IsServerConnectionException))
             || (exp is SocketException sockExp && sockExp.WithData("SocketErrorCode", sockExp.SocketErrorCode).SocketErrorCode is SocketError.HostNotFound or SocketError.HostUnreachable or SocketError.HostDown or SocketError.TimedOut)
             || (exp is HttpRequestException reqExp && reqExp.WithData("StatusCode", reqExp.StatusCode).StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout or HttpStatusCode.ServiceUnavailable or HttpStatusCode.RequestTimeout)
             || (exp is HttpProtocolException proExp && proExp.WithData("HttpRequestError", proExp.HttpRequestError).WithData("ErrorCode", proExp.ErrorCode).HttpRequestError is not HttpRequestError.UserAuthenticationError);
    }
}
