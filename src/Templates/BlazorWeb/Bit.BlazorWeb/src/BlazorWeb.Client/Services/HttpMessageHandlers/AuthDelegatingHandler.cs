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
        catch (Exception _) when (_ is ForbiddenException or UnauthorizedException)
        {
            // Notes about ForbiddenException:
            // Let's update the access token by refreshing it when a refresh token is available.
            // Following this procedure, the newly acquired access token may now include the necessary roles or claims.

            var refresh_token = await _tokenProvider.GetRefreshTokenAsync();

            if (refresh_token is not null)
            {
                var httpClient = _serviceProvider.GetRequiredService<HttpClient>();

                var refreshTokenResponse = await (await httpClient.PostAsJsonAsync("Identity/Refresh", new RefreshRequestDto { RefreshToken = refresh_token }, AppJsonContext.Default.RefreshRequestDto, cancellationToken))
                    .Content.ReadFromJsonAsync(AppJsonContext.Default.TokenResponseDto, cancellationToken: cancellationToken);

                var _jsRuntime = _serviceProvider.GetRequiredService<IJSRuntime>();
                var appAuthStateProvider = _serviceProvider.GetRequiredService<AppAuthenticationStateProvider>();

                try
                {
                    await _jsRuntime.StoreToken(refreshTokenResponse!, true);
                    await appAuthStateProvider.RaiseAuthenticationStateHasChanged();
                }
                catch (InvalidOperationException) { /* Ignore js runtime exception during pre rendering */ }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshTokenResponse!.AccessToken);

                try
                {
                    return await base.SendAsync(request, cancellationToken);
                }
                catch (Exception __) when (__ is UnauthorizedException)
                {
                    await _jsRuntime.RemoveToken();
                    await appAuthStateProvider.RaiseAuthenticationStateHasChanged();
                    throw;
                }
            }

            throw;
        }
    }
}
