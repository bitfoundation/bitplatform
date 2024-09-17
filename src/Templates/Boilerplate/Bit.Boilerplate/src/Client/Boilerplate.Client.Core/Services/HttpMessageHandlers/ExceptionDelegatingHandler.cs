﻿using System.Net;
using System.Text.Json;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public partial class ExceptionDelegatingHandler(IStringLocalizer<AppStrings> localizer, JsonSerializerOptions jsonSerializerOptions, HttpClientHandler httpClientHandler)
    : DelegatingHandler(httpClientHandler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        bool serverCommunicationSuccess = false;

        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            serverCommunicationSuccess = true;

            if (response.IsSuccessStatusCode is false && response.Content.Headers.ContentType?.MediaType?.Contains("application/json", StringComparison.InvariantCultureIgnoreCase) is true)
            {
                if (response.Headers.TryGetValues("Request-Id", out IEnumerable<string>? values) && values is not null && values.Any())
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
    }
}
