using System.Net.Http.Headers;

namespace BlazorWeb.Client.Services.HttpMessageHandlers;

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
        catch (Exception _) when ((_ is ForbiddenException or UnauthorizedException) && tokenProvider.IsInitialized)
        {
            // Let's update the access token by refreshing it when a refresh token is available.
            // Following this procedure, the newly acquired access token may now include the necessary roles or claims.

            var authManager = serviceProvider.GetRequiredService<AuthenticationManager>();
            var refresh_token = await storageService.GetItem("refresh_token");

            if (refresh_token is not null)
            {
                await authManager.RefreshToken();

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokenProvider.GetAccessTokenAsync());

                return await base.SendAsync(request, cancellationToken);
            }
            else
            {
                await authManager.SignOut();
            }

            throw;
        }
    }
}
