using System.Net.Http.Headers;

namespace BlazorWeb.Client.Services.HttpMessageHandlers;

public class AuthDelegatingHandler
    : DelegatingHandler
{
    private IAuthTokenProvider _tokenProvider = default!;
    private IJSRuntime _jsRuntime = default!;

    public AuthDelegatingHandler(IAuthTokenProvider tokenProvider, IJSRuntime jsRuntime, RetryDelegatingHandler handler)
        : base(handler)
    {
        _tokenProvider = tokenProvider;
        _jsRuntime = jsRuntime;
    }

    public AuthDelegatingHandler(IAuthTokenProvider tokenProvider, IJSRuntime jsRuntime)
        : base()
    {
        _tokenProvider = tokenProvider;
        _jsRuntime = jsRuntime;
    }

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

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (UnauthorizedException)
        {
            // try to get refresh token, store access token and refresh token,
            // then use the new access token to request's authorization header and call base.SendAsync again.
            throw;
        }
    }
}
