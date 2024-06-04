using System.Net.Http.Headers;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public class AuthDelegatingHandler(IAuthTokenProvider tokenProvider, IServiceProvider serviceProvider, IStorageService storageService, RetryDelegatingHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var access_token = await tokenProvider.GetAccessTokenAsync();
            if (access_token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            }
        }

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (Exception _) when ((_ is ForbiddenException or UnauthorizedException) &&
                                  tokenProvider.IsInitialized &&
                                  request.RequestUri?.LocalPath?.Contains("api/Identity/Refresh", StringComparison.InvariantCultureIgnoreCase) is false)
        {
            // Let's update the access token by refreshing it when a refresh token is available.
            // Following this procedure, the newly acquired access token may now include the necessary roles or claims.

            var authManager = serviceProvider.GetRequiredService<AuthenticationManager>();
            var refresh_token = await storageService.GetItem("refresh_token");

            if (refresh_token is not null)
            {
                // In the AuthenticationStateProvider, the access_token is refreshed using the refresh_token (if available).
                await authManager.RefreshToken();

                var access_token = await tokenProvider.GetAccessTokenAsync();

                if (string.IsNullOrEmpty(access_token) is false)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

                    return await base.SendAsync(request, cancellationToken);
                }
            }

            throw;
        }
    }
}
