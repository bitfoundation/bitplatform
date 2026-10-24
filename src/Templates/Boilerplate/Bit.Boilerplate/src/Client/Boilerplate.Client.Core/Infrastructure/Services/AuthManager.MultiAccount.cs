//+:cnd:noEmit

namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class AuthManager
{
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

    private static Guid? GetUserId(string? accessToken)
    {
        return GetUserId(IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false));
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        return Guid.TryParse((user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("nameid"))?.Value, out var userId) ? userId : null;
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
}
