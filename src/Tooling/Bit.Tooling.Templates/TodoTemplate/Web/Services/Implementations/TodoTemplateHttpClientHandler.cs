using System.Net.Http.Headers;
using System.Runtime.InteropServices;

namespace TodoTemplate.App.Services.Implementations;

public class TodoTemplateHttpClientHandler : HttpClientHandler
{
    private readonly ITokenProvider _tokenProvider;

    public TodoTemplateHttpClientHandler(ITokenProvider tokenProvider)
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

        return response;
    }
}
