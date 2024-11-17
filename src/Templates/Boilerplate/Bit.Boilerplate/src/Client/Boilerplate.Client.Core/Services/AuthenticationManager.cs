using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Services;

public partial class AuthenticationManager : AuthenticationStateProvider
{
    /// <summary>
    /// To prevent multiple simultaneous refresh token requests.
    /// </summary>
    private readonly SemaphoreSlim semaphore = new(1, maxCount: 1);

    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private IAuthTokenProvider tokenProvider = default!;
    [AutoInject] private IPrerenderStateService prerenderStateService;
    [AutoInject] private IExceptionHandler exceptionHandler = default!;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] private IIdentityController identityController = default!;
    [AutoInject] private ILogger<AuthenticationManager> authLogger = default!;

    /// <summary>
    /// Sign in and return whether the user requires two-factor authentication.
    /// </summary>
    /// <returns>true if the user requires two-factor authentication; otherwise, false.</returns>
    public async Task<bool> SignIn(SignInRequestDto request, CancellationToken cancellationToken)
    {
        var response = await identityController.SignIn(request, cancellationToken);

        if (response.RequiresTwoFactor) return true;

        await StoreTokens(response!, request.RememberMe);

        return false;
    }

    public async Task StoreTokens(TokenResponseDto response, bool? rememberMe = null)
    {
        if (rememberMe is null)
        {
            rememberMe = await storageService.IsPersistent("refresh_token");
        }

        await storageService.SetItem("access_token", response!.AccessToken, rememberMe is true);
        await storageService.SetItem("refresh_token", response!.RefreshToken, rememberMe is true);

        if (AppPlatform.IsBlazorHybrid is false && jsRuntime.IsInitialized())
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

        NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
    }

    public async Task SignOut(CancellationToken cancellationToken)
    {
        try
        {
            await userController.SignOut(cancellationToken);
        }
        catch (Exception exp) when (exp is ServerConnectionException or UnauthorizedException)
        {
            // The user might sign out while the app is offline, making token refresh attempts fail.
            // These exceptions are intentionally ignored in this case.
        }
        finally
        {
            await ClearTokens();
        }
    }

    public async Task<string?> RefreshToken(string requestedBy, CancellationToken cancellationToken)
    {
        try
        {
            var access_token_BeforeLockValue = await tokenProvider.GetAccessToken();
            await semaphore.WaitAsync();
            var access_token_AfterLockValue = await tokenProvider.GetAccessToken();
            if (access_token_BeforeLockValue != access_token_AfterLockValue)
                return access_token_AfterLockValue; // It was renewed by a concurrent refresh token request.
            authLogger.LogInformation("Refreshing access token requested by {RequestedBy}", requestedBy);
            try
            {
                string? refresh_token = await storageService.GetItem("refresh_token");
                if (string.IsNullOrEmpty(refresh_token))
                    throw new UnauthorizedException(localizer[nameof(AppStrings.YouNeedToSignIn)]);

                var refreshTokenResponse = await identityController.Refresh(new() { RefreshToken = refresh_token }, cancellationToken);
                await StoreTokens(refreshTokenResponse);
                return refreshTokenResponse.AccessToken!;
            }
            catch (Exception exp)
            {
                if (exp is UnauthorizedException)
                {
                    // refresh_token is either invalid or expired.
                    await ClearTokens();
                }
                authLogger.LogError(exp, "Refreshing access token requested by {RequestedBy} failed", requestedBy);
                return null;
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Handles the process of determining the user's authentication state based on the availability of access and refresh tokens.
    /// 
    /// - If no access / refresh token exists, an anonymous user object is returned to Blazor.
    /// - If an access token exists, a ClaimsPrincipal is created from it regardless of its expiration status. This ensures:
    ///   - Users can access anonymous-allowed pages without unnecessary delays caused by token refresh attempts **during app startup**.
    ///   - For protected pages, it is typical for these pages to make HTTP requests to secured APIs. In such cases, the `AuthDelegatingHandler.cs`
    ///     validates the access token and refreshes it if necessary, keeping Blazor updated with the latest authentication state.
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var access_token = await prerenderStateService.GetValue(() => tokenProvider.GetAccessToken());

            return new AuthenticationState(tokenProvider.ParseAccessToken(access_token, validateExpiry: false));
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp); // Do not throw exceptions in GetAuthenticationStateAsync. This will fault CascadingAuthenticationState's state unless NotifyAuthenticationStateChanged is called again.
            return new AuthenticationState(tokenProvider.Anonymous());
        }
    }

    private async Task ClearTokens()
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
