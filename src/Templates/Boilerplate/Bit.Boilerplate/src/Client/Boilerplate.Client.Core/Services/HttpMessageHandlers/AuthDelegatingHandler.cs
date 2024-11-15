using System.Reflection;
using System.Net.Http.Headers;
using Boilerplate.Shared.Controllers;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public partial class AuthDelegatingHandler(IJSRuntime jsRuntime,
                                           IStorageService storageService,
                                           IServiceProvider serviceProvider,
                                           IAuthTokenProvider tokenProvider,
                                           IStringLocalizer<AppStrings> localizer,
                                           AbsoluteServerAddressProvider absoluteServerAddress,
                                           HttpMessageHandler handler) : DelegatingHandler(handler)
{

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var isInternalRequest = request.RequestUri!.ToString().StartsWith(absoluteServerAddress, StringComparison.InvariantCultureIgnoreCase);

        try
        {
            if (isInternalRequest && /* We will restrict sending the access token to our own server only. */
                HasAnonymousApiAttribute(request) is false &&
                request.Headers.Authorization is null)
            {
                var access_token = await tokenProvider.GetAccessToken();
                if (access_token is not null)
                {
                    if (tokenProvider.ParseAccessToken(access_token, validateExpiry: true).IsAuthenticated() is false)
                        throw new UnauthorizedException(localizer[nameof(AppStrings.YouNeedToSignIn)]);

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

            var isRefreshTokenRequest = request.RequestUri?.LocalPath?.Contains(IIdentityController.RefreshUri, StringComparison.InvariantCultureIgnoreCase) is true;

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

    /// <summary>
    /// <see cref="AnonymousApiAttribute"/>
    /// </summary>
    private static bool HasAnonymousApiAttribute(HttpRequestMessage request)
    {
        if (request.Options.TryGetValue(new(RequestOptionNames.IControllerType), out Type? controllerType) is false)
            return false;

        var parameterTypes = ((Dictionary<string, Type>)request.Options.GetValueOrDefault(RequestOptionNames.ActionParametersInfo)!).Select(p => p.Value).ToArray();
        var method = controllerType!.GetMethod((string)request.Options.GetValueOrDefault(RequestOptionNames.ActionName)!, parameterTypes)!;
        return controllerType.GetCustomAttribute<AnonymousApiAttribute>(inherit: true) is not null ||
               method.GetCustomAttribute<AnonymousApiAttribute>() is not null;
    }
}
