using System.Reflection;
using System.Net.Http.Headers;
using Boilerplate.Client.Core.Controllers;

namespace Boilerplate.Client.Core.Services.HttpMessageHandlers;

public class AuthDelegatingHandler(IAuthTokenProvider tokenProvider, IServiceProvider serviceProvider, IStorageService storageService, RetryDelegatingHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null && HasNoAuthHeaderPolicy(request) is false)
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

    /// <summary>
    /// <see cref="NoAuthorizeHeaderPolicyAttribute"/>
    /// </summary>
    private static bool HasNoAuthHeaderPolicy(HttpRequestMessage request)
    {
        if (request.Options.TryGetValue(new(RequestOptionNames.IControllerType), out Type? controllerType) is false)
            return false;

        var parameterTypes = ((Dictionary<string, Type>)request.Options.GetValueOrDefault(RequestOptionNames.ActionParametersInfo)!).Select(p => p.Value).ToArray();
        var method = controllerType!.GetMethod((string)request.Options.GetValueOrDefault(RequestOptionNames.ActionName)!, parameterTypes)!;
        return method.GetCustomAttribute<NoAuthorizeHeaderPolicyAttribute>() is not null;
    }
}
