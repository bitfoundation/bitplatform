using System.Net.Http.Headers;
using Boilerplate.Shared.Dtos.Identity;

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
            // Notes about ForbiddenException:
            // Let's update the access token by refreshing it when a refresh token is available.
            // Following this procedure, the newly acquired access token may now include the necessary roles or claims.

            var refresh_token = await jsRuntime.GetLocalStorage("refresh_token");

            if (refresh_token is not null)
            {
                var httpClient = serviceProvider.GetRequiredService<HttpClient>();
                var appAuthStateProvider = serviceProvider.GetRequiredService<AppAuthenticationStateProvider>();

                try
                {
                    var refreshTokenResponse = await (await httpClient.PostAsJsonAsync("Identity/Refresh", new RefreshRequestDto { RefreshToken = refresh_token }, AppJsonContext.Default.RefreshRequestDto, cancellationToken))
                        .Content.ReadFromJsonAsync(AppJsonContext.Default.TokenResponseDto, cancellationToken);

                    await jsRuntime.StoreToken(refreshTokenResponse!);

                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshTokenResponse!.AccessToken);
                }
                catch (ResourceValidationException exp) /* refresh_token is expired */
                {
                    await jsRuntime.RemoveToken();
                    throw new UnauthorizedException(nameof(AppStrings.YouNeedToSignIn), exp);
                }
                finally
                {
                    await appAuthStateProvider.RaiseAuthenticationStateHasChanged();
                }

                return await base.SendAsync(request, cancellationToken);
            }

            throw;
        }
    }
}
