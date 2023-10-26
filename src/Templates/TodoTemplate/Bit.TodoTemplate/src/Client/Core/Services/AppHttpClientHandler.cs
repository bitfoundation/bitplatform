//-:cnd:noEmit
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;

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

        bool afterReceiveResponse = false;

        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            afterReceiveResponse = true;

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
        catch (Exception exp)
        {
            StringBuilder report = new();

            if (exp is HttpRequestException httpReqExp)
                report.AppendLine($"Error: {httpReqExp.HttpRequestError},Code: {(httpReqExp.StatusCode?.ToString() ?? "NULL")}");
            if (exp.InnerException is SocketException socketExp)
                report.AppendLine($"Code: {socketExp.SocketErrorCode}");
            if (exp.InnerException?.InnerException is SocketException socketExp2)
                report.AppendLine($"Code: {socketExp2.SocketErrorCode}");

            report.AppendLine($"afterReceiveResponse: {afterReceiveResponse}");
            report.AppendLine(exp.ToString());

            throw new RestException(report.ToString(), exp);
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
