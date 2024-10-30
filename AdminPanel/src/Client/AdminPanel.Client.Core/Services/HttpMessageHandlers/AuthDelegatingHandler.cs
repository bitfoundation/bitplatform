using System.Net.Http.Headers;

namespace AdminPanel.Client.Core.Services.HttpMessageHandlers;

public partial class AuthDelegatingHandler(IAuthTokenProvider tokenProvider,
    IJSRuntime jsRuntime,
    IServiceProvider serviceProvider, 
    IStorageService storageService, 
    HttpMessageHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var access_token = await tokenProvider.GetAccessToken();
            if (access_token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            }
        }

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (KnownException _) when (_ is ForbiddenException or UnauthorizedException)
        {
            // Let's update the access token by refreshing it when a refresh token is available.
            // Following this procedure, the newly acquired access token may now include the necessary roles or claims.

            if (AppPlatform.IsBlazorHybrid is false && jsRuntime.IsInitialized() is false) 
                throw; // We don't have access to refresh_token during pre-rendering.

            if (request.RequestUri?.LocalPath?.Contains("api/Identity/Refresh", StringComparison.InvariantCultureIgnoreCase) is true)
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
