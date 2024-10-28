using System.Text;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Services;

public partial class AuthenticationManager : AuthenticationStateProvider
{
    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private IAuthTokenProvider tokenProvider = default!;
    [AutoInject] private IPrerenderStateService prerenderStateService;
    [AutoInject] private IExceptionHandler exceptionHandler = default!;
    [AutoInject] private IIdentityController identityController = default!;
    [AutoInject] private JsonSerializerOptions jsonSerializerOptions = default!;

    /// <summary>
    /// Sign in and return whether the user requires two-factor authentication.
    /// </summary>
    /// <returns>true if the user requires two-factor authentication; otherwise, false.</returns>
    public async Task<bool> SignIn(SignInRequestDto request, CancellationToken cancellationToken)
    {
        var response = await identityController.SignIn(request, cancellationToken);

        if (response.RequiresTwoFactor) return true;

        await OnNewToken(response!, request.RememberMe);

        return false;
    }

    public async Task OnNewToken(TokenResponseDto response, bool? rememberMe = null)
    {
        await StoreTokens(response, rememberMe);

        var state = await GetAuthenticationStateAsync();

        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    public async Task SignOut(CancellationToken cancellationToken)
    {
        try
        {
            if (await storageService.GetItem("refresh_token") is not null)
            {
                await userController.SignOut(cancellationToken);
            }
        }
        finally
        {
            await storageService.RemoveItem("access_token");
            await storageService.RemoveItem("refresh_token");
            if (AppPlatform.IsBlazorHybrid is false)
            {
                await cookie.Remove("access_token");
            }
            NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
        }
    }

    public async Task RefreshToken()
    {
        if (AppPlatform.IsBlazorHybrid is false)
        {
            await cookie.Remove("access_token");
        }
        await storageService.RemoveItem("access_token");
        NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var access_token = await prerenderStateService.GetValue(() => tokenProvider.GetAccessToken());

            if (string.IsNullOrEmpty(access_token) && jsRuntime.IsInitialized())
            {
                string? refresh_token = await storageService.GetItem("refresh_token");

                if (string.IsNullOrEmpty(refresh_token) is false)
                {
                    // We refresh the access_token to ensure a seamless user experience, preventing unnecessary 'NotAuthorized' page redirects and improving overall UX.
                    // This method is triggered after 401 and 403 server responses in AuthDelegationHandler,
                    // as well as when accessing pages without the required permissions in NotAuthorizedPage, ensuring that any recent claims granted to the user are promptly reflected.

                    try
                    {
                        var refreshTokenResponse = await identityController.Refresh(new() { RefreshToken = refresh_token }, CancellationToken.None);
                        await StoreTokens(refreshTokenResponse!);
                        access_token = refreshTokenResponse!.AccessToken;
                    }
                    catch (UnauthorizedException) // refresh_token is either invalid or expired.
                    {
                        await storageService.RemoveItem("refresh_token");
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
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp); // Do not throw exceptions in GetAuthenticationStateAsync. This will fault CascadingAuthenticationState's state unless NotifyAuthenticationStateChanged is called again.
            return NotSignedIn();
        }
    }

    private async Task StoreTokens(TokenResponseDto response, bool? rememberMe = null)
    {
        if (rememberMe is null)
        {
            rememberMe = await storageService.IsPersistent("refresh_token");
        }

        await storageService.SetItem("access_token", response!.AccessToken, rememberMe is true);
        await storageService.SetItem("refresh_token", response!.RefreshToken, rememberMe is true);

        if (jsRuntime.IsInitialized())
        {
            await cookie.Set(new()
            {
                Name = "access_token",
                Value = response.AccessToken,
                MaxAge = rememberMe is true ? response.ExpiresIn : null, // to create a session cookie
                Path = "/",
                SameSite = SameSite.Strict,
                Secure = AppEnvironment.IsDev() is false
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
