﻿//+:cnd:noEmit
using System.Diagnostics;
using System.Net;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public partial class ExceptionDelegatingHandler(ILogger<HttpClient> logger,
                                                PubSubService pubSubService,
                                                IStringLocalizer<AppStrings> localizer,
                                                JsonSerializerOptions jsonSerializerOptions,
                                                AbsoluteServerAddressProvider absoluteServerAddress,
                                                HttpMessageHandler handler) : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending HTTP request {Method} {Uri}", request.Method, request.RequestUri);
        IEnumerable<string>? requestId = null;
        HttpStatusCode? statusCode = null;
        var stopwatch = Stopwatch.StartNew();

        bool serverCommunicationSuccess = false;
        var isInternalRequest = request.RequestUri!.ToString().StartsWith(absoluteServerAddress, StringComparison.InvariantCultureIgnoreCase);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            statusCode = response.StatusCode;
            response.Headers.TryGetValues("Request-Id", out requestId);

            serverCommunicationSuccess = true;

            if (isInternalRequest && /* The following exception handling mechanism applies exclusively to responses from our own server. */
                response.IsSuccessStatusCode is false &&
                response.Content.Headers.ContentType?.MediaType?.Contains("application/json", StringComparison.InvariantCultureIgnoreCase) is true)
            {
                RestErrorInfo restError = (await response!.Content.ReadFromJsonAsync(jsonSerializerOptions.GetTypeInfo<RestErrorInfo>(), cancellationToken))!;

                Type exceptionType = typeof(RestErrorInfo).Assembly.GetType(restError.ExceptionType!) ?? typeof(UnknownException);

                var args = new List<object?> { typeof(KnownException).IsAssignableFrom(exceptionType) ? new LocalizedString(restError.Key!, restError.Message!) : restError.Message! };

                if (exceptionType == typeof(ResourceValidationException))
                {
                    args.Add(restError.Payload);
                }

                Exception exp = (Exception)Activator.CreateInstance(exceptionType, args.ToArray())!;

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

            return response;
        }
        catch (Exception exp) when ((exp is HttpRequestException && serverCommunicationSuccess is false)
            || exp is TaskCanceledException tcExp && tcExp.InnerException is TimeoutException
            || exp is HttpRequestException { StatusCode: HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout or HttpStatusCode.ServiceUnavailable })
        {
            throw new ServerConnectionException(localizer[nameof(AppStrings.ServerConnectionException)], exp);
        }
        finally
        {
            if (isInternalRequest)
            {
                pubSubService.Publish(ClientPubSubMessages.IS_ONLINE_CHANGED, serverCommunicationSuccess);
            }

            logger.Log(statusCode is null or >= HttpStatusCode.BadRequest ? LogLevel.Warning : LogLevel.Information, "Received HTTP response for {Uri} after {Duration}ms - {StatusCode} - {RequestId}",
                        request.RequestUri,
                        stopwatch.ElapsedMilliseconds,
                        statusCode,
                        requestId);
        }
    }
}
