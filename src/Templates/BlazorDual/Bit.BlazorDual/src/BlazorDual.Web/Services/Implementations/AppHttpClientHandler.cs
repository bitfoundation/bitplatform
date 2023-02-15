//-:cnd:noEmit
using System.Net;
using System.Globalization;
using System.Net.Http.Headers;

namespace BlazorDual.Web.Services.Implementations;

public partial class AppHttpClientHandler : HttpClientHandler
{
    [AutoInject] private IAuthTokenProvider _tokenProvider = default!;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null && OperatingSystem.IsBrowser() is false)
        {
            var access_token = await _tokenProvider.GetAcccessTokenAsync();
            if (access_token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            }
        }

#if MultilingualEnabled && BlazorServer
        string cultureCookie = $"c={CultureInfo.CurrentCulture.Name}|uic={CultureInfo.CurrentCulture.Name}";
        request.Headers.Add("Cookie", $".AspNetCore.Culture={cultureCookie}");
#endif

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException();
        }

        if (!response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            if (response.Headers.TryGetValues("Request-ID", out IEnumerable<string>? values) && values is not null && values.Any())
            {
                RestExceptionPayload restError = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.RestExceptionPayload);

                Type exceptionType = typeof(RestExceptionPayload).Assembly.GetType(restError.ExceptionType) ?? typeof(UnknownException);

                var args = new List<object> { typeof(KnownException).IsAssignableFrom(exceptionType) ? new LocalizedString(restError.Key!, restError.Message!) : restError.Message };

                if (exceptionType == typeof(ResourceValidationException))
                {
                    args.Add(restError.Details);
                }

                Exception exp = (Exception)Activator.CreateInstance(exceptionType, args.ToArray());

                throw exp;
            }
        }

        response.EnsureSuccessStatusCode();

        return response;
    }
}
