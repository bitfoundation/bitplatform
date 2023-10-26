//-:cnd:noEmit
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;

namespace TodoTemplate.Client.Core.Services;

public partial class AppHttpClientHandler : HttpClientHandler
{
    [AutoInject] private IAuthTokenProvider _tokenProvider = default!;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var access_token = await _tokenProvider.GetAccessTokenAsync();
            if (access_token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            }
        }

#if MultilingualEnabled && (BlazorServer || BlazorHybrid)
        string cultureCookie = $"c={CultureInfo.CurrentCulture.Name}|uic={CultureInfo.CurrentCulture.Name}";
        request.Headers.Add("Cookie", $".AspNetCore.Culture={cultureCookie}");
#endif

        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedException();
            }

            if (response.IsSuccessStatusCode is false && response.Content.Headers.ContentType?.MediaType?.Contains("application/json", StringComparison.InvariantCultureIgnoreCase) is true)
            {
                if (response.Headers.TryGetValues("Request-ID", out IEnumerable<string>? values) && values is not null && values.Any())
                {
                    RestErrorInfo restError = (await response!.Content.ReadFromJsonAsync(AppJsonContext.Default.RestErrorInfo, cancellationToken))!;

                    Type exceptionType = typeof(RestErrorInfo).Assembly.GetType(restError!.ExceptionType!) ?? typeof(UnknownException);

                    var args = new List<object?> { typeof(KnownException).IsAssignableFrom(exceptionType) ? new LocalizedString(restError.Key!, restError.Message!) : restError.Message! };

                    if (exceptionType == typeof(ResourceValidationException))
                    {
                        args.Add(restError.Payload);
                    }

                    Exception exp = (Exception)Activator.CreateInstance(exceptionType, args.ToArray())!;

                    throw exp;
                }
            }

            response.EnsureSuccessStatusCode();

            return response;
        }
        catch (Exception exp) when (InnerExceptions(exp).OfType<SocketException>().Any(socketExp => new[] { SocketError.HostNotFound, SocketError.HostDown, SocketError.HostUnreachable, SocketError.ConnectionRefused, SocketError.NetworkDown, SocketError.NetworkUnreachable }.Contains(socketExp.SocketErrorCode))
            || exp is HttpRequestException httpReqExp && new[] { HttpStatusCode.BadGateway, HttpStatusCode.GatewayTimeout }.Contains(httpReqExp.StatusCode ?? default)
            || exp.Message.Contains("The SSL connection could not be established", StringComparison.InvariantCultureIgnoreCase)
            || exp.Message.Contains("Connection failure", StringComparison.InvariantCultureIgnoreCase /*Android App*/)
            || exp.Message.Contains("Failed to fetch", StringComparison.InvariantCultureIgnoreCase /*Chrome*/)
            || exp.Message.Contains("NetworkError when attempting to fetch resource", StringComparison.InvariantCultureIgnoreCase /*Firefox*/)
            || exp.Message.Contains("Load failed", StringComparison.InvariantCultureIgnoreCase /*Safari*/)
            || exp.Message.Contains("Could not connect to the server", StringComparison.InvariantCultureIgnoreCase) /*iOS App*/)
        {
            throw new RestException(nameof(AppStrings.UnableToConnectToServer), exp);
        }
    }

    IEnumerable<Exception> InnerExceptions(Exception exp)
    {
        while (exp.InnerException is not null)
        {
            yield return exp.InnerException;
            exp = exp.InnerException;
        }
    }
}
