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
        var logScopeData = (Dictionary<string, object?>)request.Options.GetValueOrDefault(RequestOptionNames.LogScopeData)!;
        var isInternalRequest = request.RequestUri!.ToString().StartsWith(absoluteServerAddress, StringComparison.InvariantCultureIgnoreCase);

        try
        {
            if (isInternalRequest && /* We will restrict sending the access token to our own server only. */
                request.Headers.Authorization is null)
            {
                var accessToken = await tokenProvider.GetAccessToken();
                if (string.IsNullOrEmpty(accessToken) is false && HasAuthorizedApiAttribute(request))
                {
                    if (IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: true).IsAuthenticated() is false)
                    {
                        logScopeData["ClientSideAccessTokenValidationFailed"] = true;
                        throw new UnauthorizedException(localizer[nameof(AppStrings.YouNeedToSignIn)]);
                    }
                }
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }
        catch (KnownException _) when (_ is ForbiddenException or UnauthorizedException)
        {
            if (isInternalRequest is false)
                throw;

            // Notes about ForbiddenException (403):
            // Let's update the access token by refreshing it when a refresh token is available.
            // Following this procedure, the newly acquired access token may now include the necessary roles or claims.

            if (AppPlatform.IsBlazorHybrid is false && jsRuntime.IsInitialized() is false)
                throw; // We don't have access to refreshToken during pre-rendering.

            var isRefreshTokenRequest = request.RequestUri?.LocalPath?.Contains(IIdentityController.RefreshUri, StringComparison.InvariantCultureIgnoreCase) is true;

            if (isRefreshTokenRequest)
                throw; // To prevent refresh token loop

            var refreshToken = await storageService.GetItem("refresh_token");
            if (string.IsNullOrEmpty(refreshToken)) throw;

            var authManager = serviceProvider.GetRequiredService<AuthManager>();

            logScopeData["RefreshTokenRequested"] = true;
            var accessToken = await authManager.RefreshToken(requestedBy: nameof(AuthDelegatingHandler));

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// <inheritdoc cref="AuthorizedApiAttribute"/>
    /// </summary>
    private static bool HasAuthorizedApiAttribute(HttpRequestMessage request)
    {
        if (request.Options.TryGetValue(new(RequestOptionNames.IControllerType), out Type? controllerType) is false)
            return false;

        var parameterTypes = ((Dictionary<string, Type>)request.Options.GetValueOrDefault(RequestOptionNames.ActionParametersInfo)!).Select(p => p.Value).ToArray();
        var method = controllerType!.GetMethod((string)request.Options.GetValueOrDefault(RequestOptionNames.ActionName)!, parameterTypes)!;
        return controllerType.GetCustomAttribute<AuthorizedApiAttribute>(inherit: true) is not null ||
               method.GetCustomAttribute<AuthorizedApiAttribute>() is not null;
    }
}
