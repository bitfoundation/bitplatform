using System.Net.Http.Headers;
using BlazorWeb.Shared.Dtos.Identity;

namespace BlazorWeb.Client.Services.HttpMessageHandlers;

public class AuthDelegatingHandler
    : DelegatingHandler
{
    private IAuthTokenProvider _tokenProvider = default!;
    private IServiceProvider _serviceProvider = default!;

    public AuthDelegatingHandler(IAuthTokenProvider tokenProvider, IServiceProvider serviceProvider, RetryDelegatingHandler handler)
        : base(handler)
    {
        _tokenProvider = tokenProvider;
        _serviceProvider = serviceProvider;
    }

    public AuthDelegatingHandler(IAuthTokenProvider tokenProvider, IServiceProvider serviceProvider)
        : base()
    {
        _tokenProvider = tokenProvider;
        _serviceProvider = serviceProvider;
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
                var httpClient = _serviceProvider.GetRequiredService<HttpClient>();

                var refreshTokenResponse = await (await httpClient.PostAsJsonAsync("Identity/Refresh", new RefreshRequestDto { RefreshToken = refreshToken }, AppJsonContext.Default.RefreshRequestDto, cancellationToken))
                    .Content.ReadFromJsonAsync(AppJsonContext.Default.TokenResponseDto, cancellationToken: cancellationToken);

                var _jsRuntime = _serviceProvider.GetRequiredService<IJSRuntime>();

                try
                {
                    await _jsRuntime.InvokeVoidAsync("App.setCookie", "access_token", refreshTokenResponse!.AccessToken, refreshTokenResponse.ExpiresIn, true);
                    await _jsRuntime.InvokeVoidAsync("App.setCookie", "refresh_token", refreshTokenResponse.RefreshToken, TokenResponseDto.RefreshTokenExpiresIn, true);
                    await _serviceProvider.GetRequiredService<AppAuthenticationStateProvider>().RaiseAuthenticationStateHasChanged();
                }
                catch (InvalidOperationException) { /* Ignore js runtime exception during pre rendering */ }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshTokenResponse!.AccessToken);

                return await base.SendAsync(request, cancellationToken);
            }

            throw;
        }
    }
}
