//+:cnd:noEmit

namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class AuthManager : AuthenticationStateProvider, IAsyncDisposable
{
    private Action? unsubscribe;
    private Action? unsubscribeLocalStorageChanged;

    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private ILocalHttpServer localHttpServer = default!;
    [AutoInject] private ILogger<AuthManager> authLogger = default!;
    [AutoInject] private IAuthTokenProvider tokenProvider = default!;
    [AutoInject] private ClientExceptionHandlerBase exceptionHandler = default!;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] private IIdentityController identityController = default!;
    [AutoInject] private IAuthorizationService authorizationService = default!;
    [AutoInject] private ElevatedAccessService elevatedAccessService = default!;

    public void OnInit()
    {
        // Example for method call after object instantiation with dependency injection.

        unsubscribeLocalStorageChanged = pubSubService.Subscribe(ClientAppMessages.LOCAL_STORAGE_CHANGED, async payload =>
        {
            if (payload is string key && IAuthTokenProvider.IsTokenStorageKey(key) is false)
                return;

            await NotifyActiveAccountChanged(softRestartOnSwitch: true);
        });

        //#if (signalR == true)
        unsubscribe = pubSubService.Subscribe(SharedAppMessages.SESSION_REVOKED, async payload =>
        {
            var revokedUserSessionId = payload switch
            {
                Guid id => id,
                JsonElement { ValueKind: JsonValueKind.String } json when json.TryGetGuid(out var id) => id,
                _ => (Guid?)null
            };

            var user = (await GetAuthenticationStateAsync()).User;

            if (user.IsAuthenticated() && user.GetSessionId() == revokedUserSessionId)
            {
                await SignOut(default);
            }
        });
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

        await CompleteSignIn(response!, request.RememberMe);

        return false;
    }

    public async Task CompleteSignIn(TokenResponseDto response, bool rememberMe)
    {
        if (string.IsNullOrWhiteSpace(response.AccessToken))
            return;

        var newUserId = GetUserId(response.AccessToken);

        if (newUserId is not null && await TakeAccount(newUserId.Value) is { } earlierSignIn)
        {
            await RevokeQuietly(earlierSignIn.RefreshToken);
        }

        var currentUser = (await GetAuthenticationStateAsync()).User;

        if (currentUser.IsAuthenticated())
        {
            if (GetUserId(currentUser) != newUserId)
            {
                await KeepActiveAccount();
            }
            else
            {
                try
                {
                    await SignOut(activateNextAccount: false, default);
                }
                catch (Exception exp)
                {
                    authLogger.LogWarning(exp, "Failed to sign out the previous session before signing in.");
                }
            }
        }

        await StoreTokens(response, rememberMe);
    }

    public async Task StoreTokens(TokenResponseDto response, bool? rememberMe = null)
    {
        // A response without an access token means the sign-in did not complete - typically a two-factor challenge.
        // ConfirmEmail / ConfirmPhone can answer with one: register with both an e-mail and a phone, confirm one of
        // them, turn 2FA on, then come back to confirm the other, and that confirmation's automatic sign-in is stopped
        // by the second factor it has no way to ask for. Not being signed in there is fine - the user signs in again -
        // but storing the empty response is not: on the Web client the null crosses JS interop as the string "null",
        // which is non-empty enough to get past the token provider's guard and then throws while being parsed.
        if (string.IsNullOrWhiteSpace(response.AccessToken))
            return;

        rememberMe ??= await storageService.IsPersistent("refresh_token");

        await WriteActiveTokens(response.AccessToken, response.RefreshToken, rememberMe is true);

        await NotifyActiveAccountChanged(softRestartOnSwitch: false);
    }

    public Task SignOut(CancellationToken cancellationToken) => SignOut(activateNextAccount: true, cancellationToken);

    public async Task SignOutAll(CancellationToken cancellationToken)
    {
        List<DeviceAccountDto> otherAccounts = [];

        await accountsLock.WaitAsync(CancellationToken.None);
        try
        {
            foreach (var persistent in (bool[])[true, false])
            {
                otherAccounts.AddRange(await ReadAccounts(persistent));
                await WriteAccounts(persistent, []);
            }
        }
        finally
        {
            accountsLock.Release();
        }

        await Task.WhenAll(otherAccounts.Select(account => RevokeQuietly(account.RefreshToken)));

        await SignOut(activateNextAccount: false, cancellationToken);
    }

    private async Task SignOut(bool activateNextAccount, CancellationToken cancellationToken)
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
            await ClearTokens(activateNextAccount);
        }
    }

    public async Task<DeviceAccountDto[]> GetOtherAccounts()
    {
        await accountsLock.WaitAsync();
        try
        {
            return await GetOtherAccountsCore();
        }
        finally
        {
            accountsLock.Release();
        }
    }

    //#if (notification == true)
    public async Task<Guid[]> GetDeviceSessionIds()
    {
        var accessTokens = (await GetOtherAccounts()).Select(account => account.AccessToken).Append(await tokenProvider.GetAccessToken());

        return [.. accessTokens.Select(GetSessionId).OfType<Guid>()];
    }
    //#endif

    public async Task<bool> SwitchAccount(Guid userId)
    {
        await accountsLock.WaitAsync();
        try
        {
            if (GetUserId(await storageService.GetItem("access_token")) == userId)
                return true;

            var (account, persistent) = await FindAccount(userId);

            if (account is null)
                return false;

            await RemoveAccount(userId);

            await KeepActiveAccountCore();

            await WriteActiveTokens(account.AccessToken, account.RefreshToken, persistent);

            activeAccountProfile = account.User;
        }
        finally
        {
            accountsLock.Release();
        }

        await NotifyActiveAccountChanged(softRestartOnSwitch: true);

        return true;
    }

    private UserDto? activeAccountProfile;

    public void SetActiveAccountProfile(UserDto user)
    {
        activeAccountProfile = user;
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
    /// e-mail / sms, or a WebAuthn assertion in its place (See <see cref="TryEnterElevatedAccessMode"/>).</item>
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
    /// <param name="webAuthnClientResponse">A passkey assertion offered in place of <paramref name="elevatedAccessToken"/>.</param>
    public Task<string?> RefreshToken(string requestedBy, string? elevatedAccessToken = null, bool ignoreTransientException = false
        , JsonElement? webAuthnClientResponse = null
        //#if (multitenant == true)
        , Guid? requestedTenantId = null // The id of the tenant the user is trying to switch into.
                                         //#endif
        )
    {
        // Only an argument-less refresh may be de-duplicated. A caller asking for something SPECIFIC - a tenant to
        // switch into, or an elevated access token - must get its own request, otherwise its arguments are silently
        // dropped and it is handed the result of somebody else's plain refresh (and told it succeeded).
        var hasRequestOfItsOwn = elevatedAccessToken is not null
            || webAuthnClientResponse is not null
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
                    if (string.IsNullOrWhiteSpace(refreshToken))
                        throw new UnauthorizedException(localizer[nameof(AppStrings.YouNeedToSignIn)]);

                    // On Blazor Hybrid the assertion was produced against the local http server's origin, which the
                    // server derives the relying party from.
                    var refreshTokenResponse = await identityController
                        .WithQueryIf(AppPlatform.IsBlazorHybrid && webAuthnClientResponse is not null, "origin", localHttpServer.Origin)
                        .Refresh(new()
                        {
                            RefreshToken = refreshToken,
                            ElevatedAccessToken = elevatedAccessToken,
                            WebAuthnClientResponse = webAuthnClientResponse,
                            //#if (multitenant == true)
                            RequestedTenantId = requestedTenantId,
                            //#endif
                        }, default);

                    // A sign-out or another sign-in happened meanwhile: storing this answer would bring the old session back.
                    if (await storageService.GetItem("refresh_token") != refreshToken && await IsCurrentSession(refreshTokenResponse.AccessToken) is false)
                    {
                        await KeepRefreshedAccount(refreshTokenResponse);
                        currentTsc.TrySetResult(null);
                        return;
                    }

                    await StoreTokens(refreshTokenResponse);
                    currentTsc.TrySetResult(refreshTokenResponse.AccessToken!);
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

                    if (exp is UnauthorizedException && await storageService.GetItem("refresh_token") == refreshToken) // refresh token is also invalid
                    {
                        await ClearTokens();
                    }
                    else if (exp is UnauthorizedException)
                    {
                        await ForgetAccount(refreshToken);
                    }

                    currentTsc.TrySetResult(null);
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

                currentTsc.TrySetResult(null);

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

            var user = IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false);

            if (isNotifiedUserKnown is false)
            {
                isNotifiedUserKnown = true;
                notifiedUserId = GetUserId(user);
            }

            return new AuthenticationState(user);
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp); // Do not throw exceptions in GetAuthenticationStateAsync. This will fault CascadingAuthenticationState's state unless NotifyAuthenticationStateChanged is called again.
            return new AuthenticationState(IAuthTokenProvider.Anonymous());
        }
    }

    /// <summary>
    /// Makes sure the session satisfies <see cref="AuthPolicies.ELEVATED_ACCESS"/>, collecting the proof through
    /// <see cref="ElevatedAccessService"/> if it doesn't. An already elevated session passes silently.
    /// </summary>
    public async Task<bool> TryEnterElevatedAccessMode(CancellationToken cancellationToken)
    {
        var user = IAuthTokenProvider.ParseAccessToken(await tokenProvider.GetAccessToken(), validateExpiry: true);

        if (await authorizationService.IsAuthorized(user, AuthPolicies.ELEVATED_ACCESS))
            return true;

        return await elevatedAccessService.Show(cancellationToken);
    }

    //#if (multitenant == true)
    /// <summary>
    /// Switches the user into the given tenant by refreshing the access token (See RefreshTokenRequestDto.TenantId).
    /// Passing the id of a tenant that the user doesn't have access to (or is not active) ends up kicking the user out.
    /// </summary>
    public async Task<bool> SwitchTenant(Guid tenantId, CancellationToken cancellationToken)
    {
        var accessToken = await RefreshToken(requestedBy: "SwitchTenant", requestedTenantId: tenantId);

        return string.IsNullOrWhiteSpace(accessToken) is false;
    }
    //#endif

    public async Task<string?> GetFreshAccessToken(string requestedBy, bool ignoreTransientException = false)
    {
        var accessToken = await tokenProvider.GetAccessToken();

        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        var isValid = IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: true).IsAuthenticated();

        if (isValid) return accessToken;

        return await RefreshToken(requestedBy, ignoreTransientException: ignoreTransientException);
    }

    private async Task ClearTokens(bool activateNextAccount = true)
    {
        var nextAccountActivated = false;

        await accountsLock.WaitAsync();
        try
        {
            await storageService.RemoveItem("access_token");
            await storageService.RemoveItem("refresh_token");

            if (activateNextAccount && (await GetOtherAccountsCore()).FirstOrDefault() is { } nextAccount)
            {
                var (account, persistent) = await FindAccount(nextAccount.User.Id);

                await RemoveAccount(nextAccount.User.Id);

                await WriteActiveTokens(account!.AccessToken, account.RefreshToken, persistent);

                activeAccountProfile = account.User;
                nextAccountActivated = true;
            }
        }
        finally
        {
            accountsLock.Release();
        }

        await NotifyActiveAccountChanged(softRestartOnSwitch: nextAccountActivated);
    }

    private async Task<bool> IsCurrentSession(string? accessToken)
    {
        var currentSessionId = GetSessionId(await tokenProvider.GetAccessToken());

        return currentSessionId is not null && currentSessionId == GetSessionId(accessToken);
    }

    private static Guid? GetUserId(string? accessToken)
    {
        return GetUserId(IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false));
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        return Guid.TryParse((user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("nameid"))?.Value, out var userId) ? userId : null;
    }

    private static Guid? GetSessionId(string? accessToken)
    {
        return Guid.TryParse(IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false).FindFirst(AppClaimTypes.SESSION_ID)?.Value, out var sessionId) ? sessionId : null;
    }

    private bool isNotifiedUserKnown;
    private Guid? notifiedUserId;

    private async Task NotifyActiveAccountChanged(bool softRestartOnSwitch)
    {
        var previousUserId = notifiedUserId;

        var state = await GetAuthenticationStateAsync();

        notifiedUserId = GetUserId(state.User);

        if (softRestartOnSwitch && previousUserId is not null && notifiedUserId is not null && previousUserId != notifiedUserId)
        {
            accessTokenTsc = null;
            pubSubService.Publish(ClientAppMessages.SOFT_RESTART);
        }

        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    private async Task WriteActiveTokens(string? accessToken, string? refreshToken, bool persistent)
    {
        await storageService.SetItem("refresh_token", refreshToken, persistent);
        await storageService.SetItem("access_token", accessToken, persistent);
    }

    private readonly SemaphoreSlim accountsLock = new(1, 1);

    private async Task KeepActiveAccount()
    {
        await accountsLock.WaitAsync();
        try
        {
            await KeepActiveAccountCore();
        }
        finally
        {
            accountsLock.Release();
        }
    }

    private async Task KeepActiveAccountCore()
    {
        var accessToken = await storageService.GetItem("access_token");
        var refreshToken = await storageService.GetItem("refresh_token");
        var user = IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false);

        if (GetUserId(user) is not Guid userId || string.IsNullOrWhiteSpace(refreshToken))
            return;

        await AddAccount(new()
        {
            User = activeAccountProfile?.Id == userId ? activeAccountProfile : new() { Id = userId, UserName = user.Identity?.Name, Email = user.GetEmail() },
            AccessToken = accessToken,
            RefreshToken = refreshToken
        }, await storageService.IsPersistent("refresh_token"));
    }

    private async Task KeepRefreshedAccount(TokenResponseDto refreshTokenResponse)
    {
        await accountsLock.WaitAsync();
        try
        {
            if (GetUserId(refreshTokenResponse.AccessToken) is not Guid userId)
                return;

            var (account, persistent) = await FindAccount(userId);

            if (account is null || GetSessionId(account.AccessToken) != GetSessionId(refreshTokenResponse.AccessToken))
                return;

            account.AccessToken = refreshTokenResponse.AccessToken;
            account.RefreshToken = refreshTokenResponse.RefreshToken;

            await RemoveAccount(userId);
            await AddAccount(account, persistent);
        }
        finally
        {
            accountsLock.Release();
        }
    }

    private async Task ForgetAccount(string? refreshToken)
    {
        await accountsLock.WaitAsync();
        try
        {
            if ((await GetOtherAccountsCore()).FirstOrDefault(account => account.RefreshToken == refreshToken) is { } account)
            {
                await RemoveAccount(account.User.Id);
            }
        }
        finally
        {
            accountsLock.Release();
        }
    }

    private async Task<DeviceAccountDto?> TakeAccount(Guid userId)
    {
        await accountsLock.WaitAsync();
        try
        {
            var (account, _) = await FindAccount(userId);

            await RemoveAccount(userId);

            return account;
        }
        finally
        {
            accountsLock.Release();
        }
    }

    private async Task<DeviceAccountDto[]> GetOtherAccountsCore()
    {
        var activeUserId = GetUserId(await storageService.GetItem("access_token"));

        List<DeviceAccountDto> accounts = [];

        foreach (var persistent in (bool[])[true, false])
        {
            accounts.AddRange(await ReadAccounts(persistent));
        }

        return [.. accounts.Where(account => account.User.Id != activeUserId)
                           .OrderBy(account => account.User.DisplayName, StringComparer.CurrentCultureIgnoreCase)];
    }

    private async Task<(DeviceAccountDto? Account, bool Persistent)> FindAccount(Guid userId)
    {
        foreach (var persistent in (bool[])[true, false])
        {
            if ((await ReadAccounts(persistent)).FirstOrDefault(account => account.User.Id == userId) is { } account)
                return (account, persistent);
        }

        return (null, false);
    }

    private async Task AddAccount(DeviceAccountDto account, bool persistent)
    {
        await RemoveAccount(account.User.Id);

        await WriteAccounts(persistent, [.. await ReadAccounts(persistent), account]);
    }

    private async Task RemoveAccount(Guid userId)
    {
        foreach (var persistent in (bool[])[true, false])
        {
            var accounts = await ReadAccounts(persistent);

            if (accounts.RemoveAll(account => account.User.Id == userId) > 0)
            {
                await WriteAccounts(persistent, accounts);
            }
        }
    }

    private async Task<List<DeviceAccountDto>> ReadAccounts(bool persistent)
    {
        var json = await storageService.GetItem(persistent ? "accounts" : "temp_accounts");

        if (string.IsNullOrWhiteSpace(json))
            return [];

        List<DeviceAccountDto> accounts;

        try
        {
            accounts = JsonSerializer.Deserialize(json, IdentityJsonContext.Default.ListDeviceAccountDto) ?? [];
        }
        catch (JsonException exp)
        {
            authLogger.LogWarning(exp, "Failed to read the accounts stored on this device.");
            accounts = [];
        }

        if (accounts.RemoveAll(account => IAuthTokenProvider.ParseAccessToken(account.RefreshToken, validateExpiry: true).IsAuthenticated() is false) > 0)
        {
            await WriteAccounts(persistent, accounts);
        }

        return accounts;
    }

    private async Task WriteAccounts(bool persistent, List<DeviceAccountDto> accounts)
    {
        var key = persistent ? "accounts" : "temp_accounts";

        if (accounts.Count is 0)
        {
            await storageService.RemoveItem(key);
            return;
        }

        await storageService.SetItem(key, JsonSerializer.Serialize(accounts, IdentityJsonContext.Default.ListDeviceAccountDto), persistent);
    }

    private async Task RevokeQuietly(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        try
        {
            await identityController.RevokeRefreshToken(new() { RefreshToken = refreshToken }, CancellationToken.None);
        }
        catch (Exception exp)
        {
            authLogger.LogWarning(exp, "Failed to end the session of an account that left the device.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        semaphore.Dispose();
        accountsLock.Dispose();
        unsubscribe?.Invoke();
        unsubscribeLocalStorageChanged?.Invoke();
    }
}
