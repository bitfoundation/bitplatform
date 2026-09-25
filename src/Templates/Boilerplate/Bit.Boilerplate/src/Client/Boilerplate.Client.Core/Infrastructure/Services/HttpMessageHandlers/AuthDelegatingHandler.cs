//+:cnd:noEmit
using System.Net.Http.Headers;

namespace Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

public partial class AuthDelegatingHandler(IJSRuntime jsRuntime,
                                           IStorageService storageService,
                                           IServiceProvider serviceProvider,
                                           IAuthTokenProvider tokenProvider,
                                           IStringLocalizer<AppStrings> localizer,
                                           HttpMessageHandler handler) : DelegatingHandler(handler)
{

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var logScopeData = (Dictionary<string, object?>)request.Options.GetValueOrDefault(RequestOptionNames.LogScopeData)!;
        var isInternalRequest = request.HasExternalApiAttribute() is false;

        try
        {
            if (isInternalRequest && /* The access token will be sent exclusively to the application's own server. */
                request.Headers.Authorization is null)
            {
                var accessToken = await tokenProvider.GetAccessToken();
                if (string.IsNullOrWhiteSpace(accessToken) is false && request.HasAuthorizedApiAttribute())
                {
                    if (IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: true).IsAuthenticated() is false)
                    {
                        logScopeData["ClientSideAccessTokenValidationFailed"] = true;
                        throw new UnauthorizedException(localizer[nameof(AppStrings.YouNeedToSignIn)]);
                    }
                }
                request.Headers.Authorization = string.IsNullOrWhiteSpace(accessToken) ? null : new AuthenticationHeaderValue("Bearer", accessToken);
            }
            //#if (multitenant == true)

            if (isInternalRequest)
            {
                AddTenantToUrl(request);
            }
            //#endif

            return await base.SendAsync(request, cancellationToken);
        }
        catch (KnownException _) when (_ is ForbiddenException or UnauthorizedException)
        {
            // Notes about ForbiddenException (403):
            // Let's update the access token by refreshing it when a refresh token is available.
            // Following this procedure, the newly acquired access token may now include the necessary roles or claims.

            if (isInternalRequest is false)
                throw;

            if (AppPlatform.IsBlazorHybrid is false && jsRuntime.IsInitialized() is false)
                throw; // The `refreshToken` is not accessible during the pre-rendering phase.

            var isRefreshTokenRequest = request.RequestUri?.LocalPath?.Contains(IIdentityController.RefreshUri, StringComparison.InvariantCultureIgnoreCase) is true;

            if (isRefreshTokenRequest)
                throw; // To prevent refresh token loop

            var refreshToken = await storageService.GetItem("refresh_token");
            if (string.IsNullOrWhiteSpace(refreshToken)) throw;

            var authManager = serviceProvider.GetRequiredService<AuthManager>();

            logScopeData["RefreshTokenRequested"] = true;
            var accessToken = await authManager.RefreshToken(requestedBy: nameof(AuthDelegatingHandler));

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await base.SendAsync(request, cancellationToken);
        }
    }
    //#if (multitenant == true)

    /// <summary>
    /// The server answers a signed-in caller from the tenant in its token, and an anonymous caller of the very same url from
    /// the tenant its host resolves to (See TenantProvider). The CDN edge and the browser's http cache key on the url alone,
    /// so the anonymous answer they keep, the host store's catalogue for one, would be handed to this caller as its own.
    /// With its tenant in the url the caller never asks for a url an anonymous caller asks for.
    /// </summary>
    private static void AddTenantToUrl(HttpRequestMessage request)
    {
        if (request.Method != HttpMethod.Get || request.RequestUri is not { IsAbsoluteUri: true } requestUri)
            return;

        if (IAuthTokenProvider.ParseAccessToken(request.Headers.Authorization?.Parameter, validateExpiry: false).GetTenantId() is not Guid tenantId)
            return;

        var query = requestUri.Query.TrimStart('?');

        request.RequestUri = new UriBuilder(requestUri)
        {
            Query = FormattableString.Invariant($"{query}{(query.Length is 0 ? "" : "&")}{AppResponseCacheAttribute.TenantQueryParameterName}={tenantId}")
        }.Uri;
    }
    //#endif
}
