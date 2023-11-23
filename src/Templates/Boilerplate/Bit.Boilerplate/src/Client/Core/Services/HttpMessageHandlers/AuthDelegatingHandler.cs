using System.Net.Http.Headers;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public class AuthDelegatingHandler(IAuthTokenProvider tokenProvider, IServiceProvider serviceProvider, IJSRuntime jsRuntime, RetryDelegatingHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            if (OperatingSystem.IsBrowser() is false)
            {
                // Browsers automatically send cookies, yet we still require access_token for pre-rendering purposes.
                var access_token = await tokenProvider.GetAccessTokenAsync();
                if (access_token is not null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
                }
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

            var refresh_token = await jsRuntime.GetLocalStorage("refresh_token");

            if (refresh_token is not null)
            {
                var authManager = serviceProvider.GetRequiredService<AuthenticationManager>();

                // In the AuthenticationStateProvider, the access_token is refreshed using the refresh_token (if available).
                await authManager.RaiseAuthenticationStateHasChanged();

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await jsRuntime.GetLocalStorage("access_token"));

                return await base.SendAsync(request, cancellationToken);
            }

            throw;
        }
    }
}
