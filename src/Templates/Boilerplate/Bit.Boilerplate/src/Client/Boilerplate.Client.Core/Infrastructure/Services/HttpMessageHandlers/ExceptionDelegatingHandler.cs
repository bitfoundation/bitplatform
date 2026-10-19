//+:cnd:noEmit
using System.Net;
using System.Net.Sockets;

namespace Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

public partial class ExceptionDelegatingHandler(PubSubService pubSubService,
                                                IStringLocalizer<AppStrings> localizer,
                                                JsonSerializerOptions jsonSerializerOptions,
                                                ClientExceptionHandlerBase exceptionHandler,
                                                HttpMessageHandler handler) : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var logScopeData = (Dictionary<string, object?>)request.Options.GetValueOrDefault(RequestOptionNames.LogScopeData)!;

        bool serverCommunicationSuccess = false;
        var isInternalRequest = request.HasExternalApiAttribute() is false;

        string? requestIdValue = null;

        HttpResponseMessage? response = null;

        try
        {
            try
            {
                response = await base.SendAsync(request, cancellationToken);

                if (response.Headers.TryGetValues("Request-Id", out var requestId))
                {
                    requestIdValue = requestId.First();
                    logScopeData["RequestId"] = requestIdValue;
                }

                serverCommunicationSuccess = true;

                // The server stamps every response with a Request-Id (See UseRequestIdHeader), so its presence tells an error
                // written by our own server apart from one written by something in front of it, such as a stopped IIS site,
                // a reverse proxy or a CDN, whose body is never meant for the user. The status code doesn't matter here.
                var isWrittenByOurServer = isInternalRequest &&
                    response.IsSuccessStatusCode is false &&
                    string.IsNullOrWhiteSpace(requestIdValue) is false;

                var mediaType = response.Content.Headers.ContentType?.MediaType;

                if (isWrittenByOurServer &&
                    mediaType?.Contains("application/json", StringComparison.InvariantCultureIgnoreCase) is true)
                {
                    var problemDetails = (await response.Content.ReadFromJsonAsync(jsonSerializerOptions.GetTypeInfo<AppProblemDetails>(), cancellationToken))!;

                    throw problemDetails;
                }

                if (isWrittenByOurServer &&
                    string.Equals(mediaType, "text/plain", StringComparison.InvariantCultureIgnoreCase))
                {
                    // A plain localized message, such as AttachmentController's image validation errors
                    var message = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (string.IsNullOrWhiteSpace(message) is false)
                    {
                        throw CreateRestException(response.StatusCode, message);
                    }
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
            catch (Exception exp) when (exceptionHandler.IsTransientException(exp) || (exp is HttpRequestException && serverCommunicationSuccess is false))
            {
                serverCommunicationSuccess = false; // Let's treat the server communication as failed if an exception is caught here.
                throw new TransientException(localizer[nameof(AppStrings.TransientException)], exp);
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
            response?.Dispose(); // Only ever reached on a failure; the success path returns from the inner try.
            exp.WithData("RequestId", requestIdValue ?? "?"); // Connect the exception to its corresponding request id, if one exists.
            throw;
        }
    }

    /// <summary>
    /// Keeps the exception type the status code implies, so the rest of the app reacts to it as it would to the
    /// problem details of the same status (e.g. an <see cref="UnauthorizedException"/> still asks the user to sign in),
    /// only with the message the server wrote.
    /// </summary>
    private static RestException CreateRestException(HttpStatusCode statusCode, string message)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => new BadRequestException(message),
            HttpStatusCode.Unauthorized => new UnauthorizedException(message),
            HttpStatusCode.Forbidden => new ForbiddenException(message),
            HttpStatusCode.NotFound => new ResourceNotFoundException(message),
            HttpStatusCode.Conflict => new ConflictException(message),
            HttpStatusCode.TooManyRequests => new TooManyRequestsException(message),
            _ => new RestException(message)
        };
    }
}
