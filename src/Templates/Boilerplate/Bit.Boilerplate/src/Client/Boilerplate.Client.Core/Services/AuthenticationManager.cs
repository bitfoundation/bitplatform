using System.Text;
using System.Text.Json;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Client.Core.Controllers.Identity;
using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Services;

public partial class AuthenticationManager : AuthenticationStateProvider
{
    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private IAuthTokenProvider tokenProvider = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private IIdentityController identityController = default;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] private JsonSerializerOptions jsonSerializerOptions = default!;

    public async Task<bool> SignIn(SignInRequestDto request, CancellationToken cancellationToken)
    {
        var response = await identityController.SignIn(request, cancellationToken);

        if (response.RequiresTwoFactor) return true;

        await StoreTokens(response, request.RememberMe);

        var state = await GetAuthenticationStateAsync();

        NotifyAuthenticationStateChanged(Task.FromResult(state));

        return false;
    }

    public async Task SignOut()
    {
        await storageService.RemoveItem("access_token");
        await storageService.RemoveItem("refresh_token");
        if (AppRenderMode.PrerenderEnabled && AppRenderMode.IsBlazorHybrid is false)
        {
            await cookie.Remove("access_token");
        }
        NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
    }

    public async Task RefreshToken()
    {
        if (AppRenderMode.PrerenderEnabled && AppRenderMode.IsBlazorHybrid is false)
        {
            await cookie.Remove("access_token");
        }
        await storageService.RemoveItem("access_token");
        NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var access_token = await tokenProvider.GetAccessTokenAsync();

        if (string.IsNullOrEmpty(access_token) && tokenProvider.IsInitialized)
        {
            string? refresh_token = await storageService.GetItem("refresh_token");

            if (string.IsNullOrEmpty(refresh_token) is false)
            {
                // We refresh the access_token to ensure a seamless user experience, preventing unnecessary 'NotAuthorized' page redirects and improving overall UX.
                // This method is triggered after 401 and 403 server responses in AuthDelegationHandler,
                // as well as when accessing pages without the required permissions in NotAuthorizedPage, ensuring that any recent claims granted to the user are promptly reflected.

                try
                {
                    var refreshTokenResponse = await identityController.Refresh(new() { RefreshToken = refresh_token });
                    await StoreTokens(refreshTokenResponse!);
                    access_token = refreshTokenResponse!.AccessToken;
                }
                catch (ResourceValidationException exp) // refresh_token in invalid or expired
                {
                    await storageService.RemoveItem("refresh_token");
                    throw new UnauthorizedException(localizer[nameof(AppStrings.YouNeedToSignIn)], exp);
                }
            }
        }

        if (string.IsNullOrEmpty(access_token))
        {
            return NotSignedIn();
        }

        var identity = new ClaimsIdentity(claims: ParseTokenClaims(access_token), authenticationType: "Bearer", nameType: "name", roleType: "role");

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private async Task StoreTokens(SignInResponseDto response, bool? rememberMe = null)
    {
        if (rememberMe is null)
        {
            rememberMe = await storageService.IsPersistent("refresh_token");
        }

        await storageService.SetItem("access_token", response!.AccessToken, rememberMe is true);
        await storageService.SetItem("refresh_token", response!.RefreshToken, rememberMe is true);

        if (AppRenderMode.PrerenderEnabled && AppRenderMode.IsBlazorHybrid is false)
        {
            await cookie.Set(new()
            {
                Name = "access_token",
                Value = response.AccessToken,
                MaxAge = response.ExpiresIn,
                SameSite = SameSite.Strict,
                Secure = BuildConfiguration.IsRelease()
            });
        }
    }

    private static AuthenticationState NotSignedIn()
    {
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    private IEnumerable<Claim> ParseTokenClaims(string access_token)
    {
        return ParseJwt(access_token)
            .Select(keyValue => new Claim(keyValue.Key, keyValue.Value.ToString() ?? string.Empty))
            .ToArray();
    }

    private Dictionary<string, object> ParseJwt(string access_token)
    {
        // Split the token to get the payload
        string base64UrlPayload = access_token.Split('.')[1];

        // Convert the payload from Base64Url format to Base64
        string base64Payload = ConvertBase64UrlToBase64(base64UrlPayload);

        // Decode the Base64 string to get a JSON string
        string jsonPayload = Encoding.UTF8.GetString(Convert.FromBase64String(base64Payload));

        // Deserialize the JSON string to a dictionary
        var claims = JsonSerializer.Deserialize(jsonPayload, jsonSerializerOptions.GetTypeInfo<Dictionary<string, object>>())!;

        return claims;
    }

    private static string ConvertBase64UrlToBase64(string base64Url)
    {
        base64Url = base64Url.Replace('-', '+').Replace('_', '/');

        // Adjust base64Url string length for padding
        switch (base64Url.Length % 4)
        {
            case 2:
                base64Url += "==";
                break;
            case 3:
                base64Url += "=";
                break;
        }

        return base64Url;
    }
}
