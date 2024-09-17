using System.Reflection;
using System.Net.Http.Headers;
using Boilerplate.Shared.Controllers;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public partial class AuthDelegatingHandler(IAuthTokenProvider tokenProvider, IServiceProvider serviceProvider, IStorageService storageService, RetryDelegatingHandler handler)
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
        catch (KnownException _) when (_ is ForbiddenException or UnauthorizedException)
        {
            // Let's update the access token by refreshing it when a refresh token is available.
            // Following this procedure, the newly acquired access token may now include the necessary roles or claims.

            if (tokenProvider.IsInitialized is false ||
               request.RequestUri?.LocalPath?.Contains("api/Identity/Refresh", StringComparison.InvariantCultureIgnoreCase) is true /* To prevent refresh token loop */) throw;

            var authManager = serviceProvider.GetRequiredService<AuthenticationManager>();
            var refresh_token = await storageService.GetItem("refresh_token");

            if (refresh_token is null) throw;

            // In the AuthenticationStateProvider, the access_token is refreshed using the refresh_token (if available).
            await authManager.RefreshToken();

            var access_token = await tokenProvider.GetAccessTokenAsync();

            if (string.IsNullOrEmpty(access_token)) throw;

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
