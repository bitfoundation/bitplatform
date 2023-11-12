using System.Net;

namespace TodoTemplate.Client.Core.Services.HttpMessageHandlers;

public class ExceptionDelegatingHandler
    : DelegatingHandler
{
    public ExceptionDelegatingHandler(HttpClientHandler httpClientHandler)
        : base(httpClientHandler)
    {

    }

    public ExceptionDelegatingHandler()
    {

    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        bool serverCommunicationSuccess = false;

        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            serverCommunicationSuccess = true;

            if (response.StatusCode is HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedException();
            }

            if (response.IsSuccessStatusCode is false && response.Content.Headers.ContentType?.MediaType?.Contains("application/json", StringComparison.InvariantCultureIgnoreCase) is true)
            {
                if (response.Headers.TryGetValues("Request-ID", out IEnumerable<string>? values) && values is not null && values.Any())
                {
                    RestErrorInfo restError = (await response!.Content.ReadFromJsonAsync(AppJsonContext.Default.RestErrorInfo, cancellationToken))!;

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

            response.EnsureSuccessStatusCode();

            return response;
        }
        catch (Exception exp) when ((exp is HttpRequestException && serverCommunicationSuccess is false)
            || exp is TaskCanceledException tcExp && tcExp.InnerException is TimeoutException)
        {
            throw new ServerConnectionException(nameof(AppStrings.ServerConnectionException), exp);
        }
    }
}
