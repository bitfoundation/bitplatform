using System.Net.Http.Headers;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public partial class AuthDelegatingHandler(IAuthTokenProvider tokenProvider,
    IJSRuntime jsRuntime,
    IServiceProvider serviceProvider,
    IStorageService storageService,
    HttpMessageHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var isRefreshTokenRequest = request.RequestUri?.LocalPath?.Contains(IIdentityController.RefreshUri, StringComparison.InvariantCultureIgnoreCase) is true;

        try
        {
            if (request.Headers.Authorization is null && isRefreshTokenRequest is false)
            {
                var access_token = await tokenProvider.GetAccessToken();
                if (access_token is not null)
                {
                    if (tokenProvider.ParseAccessToken(access_token).IsAuthenticated() is false)
                        throw new UnauthorizedException();

                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
        catch (KnownException _) when (_ is ForbiddenException or UnauthorizedException)
        {
            // Let's update the access token by refreshing it when a refresh token is available.
            // Following this procedure, the newly acquired access token may now include the necessary roles or claims.

            if (AppPlatform.IsBlazorHybrid is false && jsRuntime.IsInitialized() is false)
                throw; // We don't have access to refresh_token during pre-rendering.

            if (isRefreshTokenRequest)
                throw; // To prevent refresh token loop

            var refresh_token = await storageService.GetItem("refresh_token");
            if (refresh_token is null) throw;

            var authManager = serviceProvider.GetRequiredService<AuthenticationManager>();

            // In the AuthenticationStateProvider, the access_token is refreshed using the refresh_token (if available).
            await authManager.RefreshToken();

            var access_token = await tokenProvider.GetAccessToken();

            if (string.IsNullOrEmpty(access_token)) throw;

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
