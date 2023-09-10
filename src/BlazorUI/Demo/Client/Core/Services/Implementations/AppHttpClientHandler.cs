using System.Net;

namespace Bit.BlazorUI.Demo.Client.Core.Services.Implementations;

public partial class AppHttpClientHandler : HttpClientHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
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
            if (response.Headers.TryGetValues("Request-ID", out var values) && values is not null && values.Any())
            {
                var restError = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.RestErrorInfo);

                ArgumentNullException.ThrowIfNull(nameof(restError));

                var exceptionType = typeof(RestErrorInfo).Assembly.GetType(restError?.ExceptionType ?? string.Empty) ?? typeof(UnknownException);

                List<object> args = new()
                {
                    typeof(KnownException).IsAssignableFrom(exceptionType)
                        ? new LocalizedString(restError!.Key ?? string.Empty, restError.Message ?? string.Empty)
                        : restError!.Message ?? string.Empty
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
