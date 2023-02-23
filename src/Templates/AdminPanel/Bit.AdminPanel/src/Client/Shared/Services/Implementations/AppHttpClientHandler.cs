//-:cnd:noEmit
using System.Net;
using System.Net.Http.Headers;
#if MultilingualEnabled && (BlazorServer || BlazorHybrid)
using System.Globalization;
#endif

namespace AdminPanel.Client.Shared.Services.Implementations;

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

#if MultilingualEnabled && (BlazorServer || BlazorHybrid)
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
                RestExceptionPayload restError = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.RestExceptionPayload, cancellationToken) ?? new();

                Type exceptionType = typeof(RestExceptionPayload).Assembly.GetType(restError.ExceptionType ?? string.Empty) ?? typeof(UnknownException);

                List<object> args = new()
                {
                    typeof(KnownException).IsAssignableFrom(exceptionType)
                        ? new LocalizedString(restError.Key ?? string.Empty, restError.Message ?? string.Empty)
                        : restError.Message ?? string.Empty
                };

                if (exceptionType == typeof(ResourceValidationException))
                {
                    args.Add(restError.Payload);
                }

                throw (Exception)(Activator.CreateInstance(exceptionType, args.ToArray()) ?? new Exception());
            }
        }

        response.EnsureSuccessStatusCode();

        return response;
    }
}
