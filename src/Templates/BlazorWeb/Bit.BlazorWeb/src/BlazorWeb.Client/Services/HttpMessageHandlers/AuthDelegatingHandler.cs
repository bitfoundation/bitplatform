using System.Net.Http.Headers;
using BlazorWeb.Shared.Dtos.Identity;

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
            var refreshToken = await _tokenProvider.GetRefreshTokenAsync();

            if (refreshToken is not null)
            {
                HttpRequestMessage refreshRequest = new(HttpMethod.Post, "api/Identity/Refresh")
                {
                    Content = JsonContent.Create(new RefreshRequestDto { RefreshToken = refreshToken }, AppJsonContext.Default.RefreshRequestDto)
                };

                var refreshTokenResponse = await (await base.SendAsync(refreshRequest, cancellationToken))
                    .EnsureSuccessStatusCode().Content.ReadFromJsonAsync(AppJsonContext.Default.TokenResponseDto, cancellationToken: cancellationToken);
                try
                {
                    await _jsRuntime.InvokeVoidAsync("App.setCookie", "access_token", refreshTokenResponse!.AccessToken, refreshTokenResponse.ExpiresIn, true);
                    await _jsRuntime.InvokeVoidAsync("App.setCookie", "refresh_token", refreshTokenResponse.RefreshToken, TokenResponseDto.RefreshTokenExpiresIn, true);
                }
                catch (InvalidOperationException) { }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshTokenResponse!.AccessToken);

                return await base.SendAsync(request, cancellationToken);
            }

            throw;
        }
    }
}
