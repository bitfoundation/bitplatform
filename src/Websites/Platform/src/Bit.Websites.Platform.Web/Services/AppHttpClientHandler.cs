﻿using System.Net;

namespace Bit.Websites.Platform.Web.Services;

public partial class AppHttpClientHandler : HttpClientHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
#if MultilingualEnabled && BlazorServer
        string cultureCookie = $"c={CultureInfo.CurrentCulture.Name}|uic={CultureInfo.CurrentCulture.Name}";
        request.Headers.Add("Cookie", $".AspNetCore.Culture={cultureCookie}");
#endif

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException();
        }

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
