using System.Net.Http.Headers;
using System.Runtime.InteropServices;

namespace AdminPanelTemplate.App.Services.Implementations;

public class AppHttpClientHandler : HttpClientHandler
{
    private readonly IAuthTokenProvider _tokenProvider;

    public AppHttpClientHandler(IAuthTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null && RuntimeInformation.ProcessArchitecture != Architecture.Wasm)
        {
            var access_token = await _tokenProvider.GetAcccessToken();
            if (access_token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            if (response.Headers.TryGetValues("Request-ID", out IEnumerable<string>? values) && values is not null && values.Any())
            {
                RestExceptionPayload restError = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.RestExceptionPayload);

                Type exceptionType = typeof(RestExceptionPayload).Assembly.GetType(restError.ExceptionType) ?? typeof(UnknownException);

                Exception exp = (Exception)Activator.CreateInstance(exceptionType, args: new object[] { restError.Message });

                if (exp is ResourceValidationException resValidationException)
                {
                    resValidationException.Details = restError.Details;
                }

                throw exp;
            }
        }

        response.EnsureSuccessStatusCode();

        return response;
    }
}
