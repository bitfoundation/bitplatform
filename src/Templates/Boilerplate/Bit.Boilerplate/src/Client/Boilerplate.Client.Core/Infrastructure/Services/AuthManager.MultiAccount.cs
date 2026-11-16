//+:cnd:noEmit

namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class AuthManager
{
    public async Task SignOutAll(CancellationToken cancellationToken)
    {
        var otherAccounts = await GetOtherAccounts();

        foreach (var account in otherAccounts)
        {
            await RemoveAccount(account.UserId);
        }

        await Task.WhenAll(otherAccounts.Select(account => RevokeQuietly(account.RefreshToken)));

        await SignOut(activateNextAccount: false, cancellationToken);
    }

    public async Task<DeviceAccountDto[]> GetOtherAccounts()
    {
        var activeUserId = GetUserId(await storageService.GetItem("access_token"));

        List<DeviceAccountDto> accounts = [];

        foreach (var persistent in (bool[])[true, false])
        {
            accounts.AddRange(await ReadAccounts(persistent));
        }

        return [.. accounts.Where(account => account.UserId != activeUserId)
                           .OrderBy(account => account.DisplayName, StringComparer.CurrentCultureIgnoreCase)];
    }

    //#if (notification == true)
    public async Task<Guid[]> GetDeviceSessionIds()
    {
        var sessionIds = (await GetOtherAccounts()).Select(account => account.SessionId).Append(GetSessionId(await tokenProvider.GetAccessToken()));

        return [.. sessionIds.OfType<Guid>()];
    }
    //#endif

    public async Task<bool> SwitchAccount(Guid userId, string? pin = null)
    {
        if (GetUserId(await storageService.GetItem("access_token")) == userId)
            return true;

        var (account, persistent) = await FindAccount(userId);

        if (account is null)
            return false;

        var tokens = await OpenTokens(account, pin);

        if (tokens is null)
            return false;

        await KeepActiveAccount();

        await RemoveAccount(userId);

        await WriteActiveTokens(tokens.AccessToken, tokens.RefreshToken, persistent);

        await WritePinLock(account.PinLock, persistent);

        activeAccountProfile = account;

        await NotifyActiveAccountChanged(softRestartOnSwitch: true);

        return true;
    }

    public ValueTask<bool> IsPinSupported() => pinCipher.IsSupported();

    public async Task<bool> HasPin()
    {
        return await ReadPinLock() is not null;
    }

    public async Task SetPin(string pin)
    {
        if (string.IsNullOrWhiteSpace(await storageService.GetItem("refresh_token")))
            throw new UnauthorizedException(localizer[nameof(AppStrings.YouNeedToSignIn)]);

        await WritePinLock(await pinCipher.CreateLock(pin), await storageService.IsPersistent("refresh_token"));
    }

    public async Task RemovePin()
    {
        await storageService.RemoveItem("pin_lock");
    }

    private DeviceAccountDto? activeAccountProfile;

    public void SetActiveAccountProfile(UserDto user)
    {
        activeAccountProfile = new()
        {
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FullName = user.FullName,
            HasProfilePicture = user.HasProfilePicture,
            Version = user.Version
        };
    }

    private static Guid? GetUserId(string? accessToken)
    {
        return GetUserId(IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false));
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        return Guid.TryParse((user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("nameid"))?.Value, out var userId) ? userId : null;
    }

    // False until the first GetAuthenticationStateAsync records the user the app started with.
    private bool isNotifiedUserKnown;
    private Guid? notifiedUserId;
    //#if (multitenant == true)
    private Guid? notifiedTenantId;
    //#endif

    private async Task NotifyActiveAccountChanged(bool softRestartOnSwitch)
    {
        var previousUserId = notifiedUserId;
        //#if (multitenant == true)
        var previousTenantId = notifiedTenantId;
        //#endif

        var state = await GetAuthenticationStateAsync();

        if (GetUserId(state.User) is Guid userId)
        {
            notifiedUserId = userId;

            var isSwitch = previousUserId is not null && previousUserId != userId;
            //#if (multitenant == true)
            notifiedTenantId = state.User.GetTenantId();
            // A tenant switch keeps the user id, so the check above misses it, yet every page loaded before it shows
            // the previous tenant's data while its next requests already go out with the new tenant's token.
            // The tenant id is compared rather than the token, so a plain refresh in another tab restarts nothing.
            // previousUserId == userId also rules out the first notification, when nothing was known yet.
            isSwitch |= previousUserId == userId && previousTenantId != notifiedTenantId;
            //#endif

            if (softRestartOnSwitch && isSwitch)
            {
                accessTokenTsc = null;
                pubSubService.Publish(ClientAppMessages.SOFT_RESTART);
            }
        }

        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    private async Task KeepActiveAccount()
    {
        var accessToken = await storageService.GetItem("access_token");
        var refreshToken = await storageService.GetItem("refresh_token");
        var user = IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false);

        if (GetUserId(user) is not Guid userId || string.IsNullOrWhiteSpace(refreshToken))
            return;

        var profile = activeAccountProfile?.UserId == userId ? activeAccountProfile : null;

        DeviceAccountDto account = new()
        {
            UserId = userId,
            UserName = profile is null ? user.Identity?.Name : profile.UserName,
            Email = profile is null ? user.GetEmail() : profile.Email,
            PhoneNumber = profile?.PhoneNumber,
            FullName = profile?.FullName,
            HasProfilePicture = profile?.HasProfilePicture ?? false,
            Version = profile?.Version
        };

        await SetAccountTokens(account, accessToken, refreshToken, await ReadPinLock());

        await AddAccount(account, await storageService.IsPersistent("refresh_token"));
    }

    private async Task<bool> StoreRefreshedTokens(string? sentRefreshToken, TokenResponseDto refreshTokenResponse)
    {
        if (await storageService.GetItem("refresh_token") == sentRefreshToken || await IsCurrentSession(refreshTokenResponse.AccessToken))
        {
            await StoreTokens(refreshTokenResponse);
            return true;
        }

        if (GetUserId(refreshTokenResponse.AccessToken) is not Guid userId)
            return false;

        var (account, persistent) = await FindAccount(userId);

        if (account?.SessionId is not Guid sessionId || sessionId != GetSessionId(refreshTokenResponse.AccessToken))
            return false;

        await SetAccountTokens(account, refreshTokenResponse.AccessToken, refreshTokenResponse.RefreshToken, account.PinLock);

        await AddAccount(account, persistent);

        return false;
    }

    private async Task SetAccountTokens(DeviceAccountDto account, string? accessToken, string? refreshToken, DevicePinLockDto? pinLock)
    {
        account.SessionId = GetSessionId(accessToken);
        account.RefreshTokenExpiresOn = GetExpiry(refreshToken);
        account.PinLock = pinLock;

        if (pinLock is null)
        {
            account.AccessToken = accessToken;
            account.RefreshToken = refreshToken;
            account.SealedTokens = null;
            return;
        }

        var tokens = new TokenResponseDto { AccessToken = accessToken, RefreshToken = refreshToken };

        account.AccessToken = account.RefreshToken = null;
        account.SealedTokens = await pinCipher.Seal(pinLock, JsonSerializer.SerializeToUtf8Bytes(tokens, IdentityJsonContext.Default.TokenResponseDto));
    }

    private async Task<TokenResponseDto?> OpenTokens(DeviceAccountDto account, string? pin)
    {
        if (account.PinLock is null)
            return new() { AccessToken = account.AccessToken, RefreshToken = account.RefreshToken };

        if (string.IsNullOrEmpty(pin) || account.SealedTokens is null)
            return null;

        var tokens = await pinCipher.Open(account.PinLock, pin, account.SealedTokens);

        return tokens is null ? null : JsonSerializer.Deserialize(tokens, IdentityJsonContext.Default.TokenResponseDto);
    }

    private async Task<DevicePinLockDto?> ReadPinLock()
    {
        var json = await storageService.GetItem("pin_lock");

        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize(json, IdentityJsonContext.Default.DevicePinLockDto);
        }
        catch (JsonException exp)
        {
            authLogger.LogWarning(exp, "Failed to read the PIN of the active account.");
            return null;
        }
    }

    private async Task WritePinLock(DevicePinLockDto? pinLock, bool persistent)
    {
        if (pinLock is null)
        {
            await storageService.RemoveItem("pin_lock");
            return;
        }

        await storageService.SetItem("pin_lock", JsonSerializer.Serialize(pinLock, IdentityJsonContext.Default.DevicePinLockDto), persistent);
    }

    private async Task ForgetAccount(string? refreshToken)
    {
        if (GetSessionId(refreshToken) is Guid sessionId
            && (await GetOtherAccounts()).FirstOrDefault(account => account.SessionId == sessionId) is { } account)
        {
            await RemoveAccount(account.UserId);
        }
    }

    private async Task<DeviceAccountDto?> TakeAccount(Guid userId)
    {
        var (account, _) = await FindAccount(userId);

        await RemoveAccount(userId);

        return account;
    }

    private async Task<(DeviceAccountDto? Account, bool Persistent)> FindAccount(Guid userId)
    {
        foreach (var persistent in (bool[])[true, false])
        {
            if ((await ReadAccounts(persistent)).FirstOrDefault(account => account.UserId == userId) is { } account)
                return (account, persistent);
        }

        return (null, false);
    }

    private async Task AddAccount(DeviceAccountDto account, bool persistent)
    {
        await RemoveAccount(account.UserId);

        await WriteAccounts(persistent, [.. await ReadAccounts(persistent), account]);
    }

    private async Task RemoveAccount(Guid userId)
    {
        foreach (var persistent in (bool[])[true, false])
        {
            var accounts = await ReadAccounts(persistent);

            if (accounts.RemoveAll(account => account.UserId == userId) > 0)
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

        var now = TimeProvider.GetUtcNow();

        if (accounts.RemoveAll(account => account.RefreshTokenExpiresOn is not DateTimeOffset expiresOn || expiresOn <= now) > 0)
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

    private static DateTimeOffset? GetExpiry(string? token)
    {
        return long.TryParse(IAuthTokenProvider.ParseAccessToken(token, validateExpiry: false).FindFirst("exp")?.Value, out var expSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(expSeconds)
            : null;
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
}
