//+:cnd:noEmit

namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class AuthManager : AuthenticationStateProvider, IAsyncDisposable
{
    private Action? unsubscribe;

    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private PromptService promptService = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private ILogger<AuthManager> authLogger = default!;
    [AutoInject] private IAuthTokenProvider tokenProvider = default!;
    [AutoInject] private ClientExceptionHandlerBase exceptionHandler = default!;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] private IIdentityController identityController = default!;
    [AutoInject] private IAuthorizationService authorizationService = default!;

    public void OnInit()
    {
        // Example for method call after object instantiation with dependency injection.

        //#if (signalR == true)
        unsubscribe = pubSubService.Subscribe(SharedAppMessages.SESSION_REVOKED, _ => SignOut(default));
        //#endif
    }

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
        // A response without an access token means the sign-in did not complete - typically a two-factor challenge.
        // ConfirmEmail / ConfirmPhone can answer with one: register with both an e-mail and a phone, confirm one of
        // them, turn 2FA on, then come back to confirm the other, and that confirmation's automatic sign-in is stopped
        // by the second factor it has no way to ask for. Not being signed in there is fine - the user signs in again -
        // but storing the empty response is not: on the Web client the null crosses JS interop as the string "null",
        // which is non-empty enough to get past the token provider's guard and then throws while being parsed.
        if (string.IsNullOrEmpty(response.AccessToken))
            return;

        rememberMe ??= await storageService.IsPersistent("refresh_token");

        await storageService.SetItem("access_token", response!.AccessToken);
        await storageService.SetItem("refresh_token", response!.RefreshToken, rememberMe is true);

        NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
    }

    public async Task SignOut(CancellationToken cancellationToken)
    {
        try
        {
            await userController.SignOut(cancellationToken);
        }
        catch (Exception exp) when (exp is TransientException or UnauthorizedException or ResourceNotFoundException or ClientNotSupportedException)
        {
            // If the client's access token is expired, the client would attempt to refresh it,
            // but if the client is offline or outdated, the refresh token request will fail.
            // These exceptions are intentionally ignored in these cases.
        }
        finally
        {
            await ClearTokens();
        }
    }

    /// <summary>
    /// To prevent multiple simultaneous refresh token requests.
    /// </summary>
    private SemaphoreSlim semaphore = new(1, 1);
    private TaskCompletionSource<string?>? accessTokenTsc = null;

    /// <summary>
    /// Gets a new access token from the server, and is the way the app performs <b>anything that changes the user's
    /// claims without signing in again</b> - not only renewing an expired token:
    /// <list type="bullet">
    /// <item>a plain renewal, when the current access token is expired or about to be (See AuthDelegatingHandler).</item>
    /// <item>raising the session to an <b>elevated (privileged)</b> one, by passing the token the user received by
    /// e-mail / sms (See <see cref="RequestElevatedAccess"/>).</item>
    /// <item><b>switching the active tenant</b>, by passing the id of the tenant to enter (See SwitchTenant).</item>
    /// </list>
    /// They all go through this one method because the server answers all of them from the same endpoint: it validates
    /// the refresh token, applies whatever the request asked for, and returns a new access token carrying the resulting
    /// claims - which is also why the returned token, not the boolean the caller gets back, is the source of truth for
    /// what actually happened. Anything added later that changes the claims of a signed-in user belongs here too.
    /// <para>
    /// Concurrent callers are de-duplicated: while a plain refresh is in flight, another plain refresh joins it instead
    /// of issuing a second request. A call that carries arguments of its own never joins one, because it would
    /// otherwise be handed an answer to somebody else's question - reporting success while its arguments were silently
    /// dropped.
    /// </para>
    /// </summary>
    /// <param name="requestedBy">Free text, for logs and error reports only. It has no effect on the request.</param>
    public Task<string?> RefreshToken(string requestedBy, string? elevatedAccessToken = null, bool ignoreTransientException = false
        //#if (multitenant == true)
        , Guid? requestedTenantId = null // The id of the tenant the user is trying to switch into.
                                         //#endif
        )
    {
        // Only an argument-less refresh may be de-duplicated. A caller asking for something SPECIFIC - a tenant to
        // switch into, or an elevated access token - must get its own request, otherwise its arguments are silently
        // dropped and it is handed the result of somebody else's plain refresh (and told it succeeded).
        var hasRequestOfItsOwn = elevatedAccessToken is not null
            //#if (multitenant == true)
            || requestedTenantId is not null
            //#endif
            ;

        if (hasRequestOfItsOwn is false && accessTokenTsc is not null)
            return accessTokenTsc.Task;

        // RunContinuationsAsynchronously is load bearing: without it SetResult below resumes the awaiting caller
        // synchronously, in the middle of this method, so the `finally` that clears the field has not run yet - and a
        // caller that awaits one refresh and immediately starts another finds a non-null, already-completed field.
        var tsc = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (hasRequestOfItsOwn is false)
        {
            accessTokenTsc = tsc;
        }

        _ = RefreshTokenImplementation(tsc);

        return tsc.Task;

        async Task RefreshTokenImplementation(TaskCompletionSource<string?> currentTsc)
        {
            try
            {
                await semaphore.WaitAsync();
                authLogger.LogInformation("Refreshing access token requested by {RequestedBy}", requestedBy);
                string? refreshToken = await storageService.GetItem("refresh_token");
                try
                {
                    if (string.IsNullOrEmpty(refreshToken))
                        throw new UnauthorizedException(localizer[nameof(AppStrings.YouNeedToSignIn)]);

                    var refreshTokenResponse = await identityController.Refresh(new()
                    {
                        RefreshToken = refreshToken,
                        ElevatedAccessToken = elevatedAccessToken,
                        //#if (multitenant == true)
                        RequestedTenantId = requestedTenantId,
                        //#endif
                    }, default);
                    await StoreTokens(refreshTokenResponse);
                    currentTsc.SetResult(refreshTokenResponse.AccessToken!);
                }
                catch (Exception exp)
                {
                    if (exp is not TransientException || ignoreTransientException is false)
                    {
                        exceptionHandler.Handle(exp, parameters: new()
                        {
                            { "AdditionalData", "Refreshing access token failed." },
                            { "RefreshTokenRequestedBy", requestedBy }
                        }, displayKind: ExceptionDisplayKind.NonInterrupting);
                    }

                    if (exp is UnauthorizedException) // refresh token is also invalid
                    {
                        await ClearTokens();
                    }

                    currentTsc.SetResult(null);
                }
            }
            finally
            {
                // Only if this call is still the one the field points at - a request with arguments of its own never
                // owned the field, and must not clear a plain refresh that started in the meantime.
                if (ReferenceEquals(accessTokenTsc, currentTsc))
                {
                    accessTokenTsc = null;
                }
                semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Handles the process of determining the user's authentication state based on the availability of access and refresh tokens.
    ///
    /// - If no access / refresh token exists, an anonymous user object is returned to Blazor.
    /// - If an access token exists, a ClaimsPrincipal is created from it regardless of its expiration status. This ensures:
    ///   - Users can access anonymous-allowed pages without unnecessary delays caused by token refresh attempts **during app startup**.
    ///   - For protected pages, it is typical for these pages to make HTTP requests to secured APIs. In such cases, the `<see cref="AuthDelegatingHandler"/>`
    ///     validates the access token and refreshes it if necessary, keeping Blazor updated with the latest authentication state.
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var accessToken = await tokenProvider.GetAccessToken();

            return new AuthenticationState(IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false));
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp); // Do not throw exceptions in GetAuthenticationStateAsync. This will fault CascadingAuthenticationState's state unless NotifyAuthenticationStateChanged is called again.
            return new AuthenticationState(IAuthTokenProvider.Anonymous());
        }
    }

    public async Task<bool> TryEnterElevatedAccessMode(CancellationToken cancellationToken)
    {
        var user = IAuthTokenProvider.ParseAccessToken(await tokenProvider.GetAccessToken(), validateExpiry: true);
        var hasElevatedAccess = await authorizationService.IsAuthorized(user, AuthPolicies.ELEVATED_ACCESS);
        if (hasElevatedAccess)
            return true;

        try
        {
            await userController.SendElevatedAccessToken(cancellationToken);
        }
        catch (TooManyRequestsException exp)
        {
            exceptionHandler.Handle(exp, displayKind: ExceptionDisplayKind.NonInterrupting); // Let's show prompt anyway.
        }

        var token = await promptService.Show(localizer[AppStrings.EnterElevatedAccessToken], title: "Boilerplate", otpInput: true);
        if (string.IsNullOrEmpty(token))
            return false;

        var accessToken = await RefreshToken(requestedBy: "RequestElevatedAccess", token);
        return string.IsNullOrEmpty(accessToken) is false;
    }

    //#if (multitenant == true)
    /// <summary>
    /// Switches the user into the given tenant by refreshing the access token (See RefreshTokenRequestDto.TenantId).
    /// Passing the id of a tenant that the user doesn't have access to (or is not active) ends up kicking the user out.
    /// </summary>
    public async Task<bool> SwitchTenant(Guid tenantId, CancellationToken cancellationToken)
    {
        var accessToken = await RefreshToken(requestedBy: "SwitchTenant", requestedTenantId: tenantId);

        return string.IsNullOrEmpty(accessToken) is false;
    }
    //#endif

    public async Task<string?> GetFreshAccessToken(string requestedBy, bool ignoreTransientException = false)
    {
        var accessToken = await tokenProvider.GetAccessToken();

        if (string.IsNullOrEmpty(accessToken))
            return null;

        var isValid = IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: true).IsAuthenticated();

        if (isValid) return accessToken;

        return await RefreshToken(requestedBy, ignoreTransientException: ignoreTransientException);
    }

    private async Task ClearTokens()
    {
        await storageService.RemoveItem("access_token");
        await storageService.RemoveItem("refresh_token");
        NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
    }

    public async ValueTask DisposeAsync()
    {
        semaphore.Dispose();
        unsubscribe?.Invoke();
    }
}
