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
    [AutoInject] private IIdentityController identityController = default!;

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
        NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var access_token = await prerenderStateService.GetValue(() => tokenProvider.GetAccessToken());

            bool inPrerenderSession = AppPlatform.IsBlazorHybrid is false && jsRuntime.IsInitialized() is false;

            if (string.IsNullOrEmpty(access_token) && inPrerenderSession is false)
            {
                try
                {
                    await semaphore.WaitAsync();
                    access_token = await tokenProvider.GetAccessToken();
                    if (string.IsNullOrEmpty(access_token)) // Check again after acquiring the lock.
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
                                if (AppPlatform.IsBlazorHybrid is false)
                                {
                                    await cookie.Remove("access_token");
                                }
                                await storageService.RemoveItem("access_token");
                                await storageService.RemoveItem("refresh_token");
                            }
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }

            return new AuthenticationState(tokenProvider.ParseAccessToken(access_token, validateExpiry: false /* For better UX in order to minimize Routes.razor's Authorizing loading duration. */));
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp); // Do not throw exceptions in GetAuthenticationStateAsync. This will fault CascadingAuthenticationState's state unless NotifyAuthenticationStateChanged is called again.
            return new AuthenticationState(tokenProvider.Anonymous());
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
    }
}
