﻿using System.Net;
using System.Globalization;
using System.Net.Http.Headers;

namespace BitCareers.Web.Services.Implementations;

public partial class AppHttpClientHandler : HttpClientHandler
{
    [AutoInject] private IAuthTokenProvider _tokenProvider = default!;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var access_token = await _tokenProvider.GetAcccessTokenAsync();
            if (access_token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode is false && response.Content.Headers.ContentType?.MediaType?.Contains("application/json", StringComparison.InvariantCultureIgnoreCase) is true)
        {
            if (response.Headers.TryGetValues("Request-ID", out IEnumerable<string>? values) && values is not null && values.Any())
            {
                RestErrorInfo restError = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.RestErrorInfo);

                Type exceptionType = typeof(RestErrorInfo).Assembly.GetType(restError.ExceptionType) ?? typeof(UnknownException);

                var args = new List<object> { typeof(KnownException).IsAssignableFrom(exceptionType) ? new LocalizedString(restError.Key!, restError.Message!) : restError.Message };

                if (exceptionType == typeof(ResourceValidationException))
                {
                    args.Add(restError.Payload);
                }

                Exception exp = (Exception)Activator.CreateInstance(exceptionType, args.ToArray());

                throw exp;
            }
        }

        response.EnsureSuccessStatusCode();

        return response;
    }
}
