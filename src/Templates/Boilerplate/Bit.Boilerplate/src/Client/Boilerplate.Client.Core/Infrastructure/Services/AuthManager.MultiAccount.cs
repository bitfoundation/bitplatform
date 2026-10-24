//+:cnd:noEmit

namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class AuthManager
{
    public async Task SignOutAll(CancellationToken cancellationToken)
    {
        var otherAccounts = await GetOtherAccounts();

        foreach (var account in otherAccounts)
        {
            await RemoveAccount(account.User.Id);
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

        return [.. accounts.Where(account => account.User.Id != activeUserId)
                           .OrderBy(account => account.User.DisplayName, StringComparer.CurrentCultureIgnoreCase)];
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
        if (GetUserId(await storageService.GetItem("access_token")) == userId)
            return true;

        var (account, persistent) = await FindAccount(userId);

        if (account is null)
            return false;

        await RemoveAccount(userId);

        await KeepActiveAccount();

        await WriteActiveTokens(account.AccessToken, account.RefreshToken, persistent);

        activeAccountProfile = account.User;

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

    // False until the first GetAuthenticationStateAsync records the user the app started with.
    private bool isNotifiedUserKnown;
    private Guid? notifiedUserId;

    private async Task NotifyActiveAccountChanged(bool softRestartOnSwitch)
    {
        var previousUserId = notifiedUserId;

        var state = await GetAuthenticationStateAsync();

        if (GetUserId(state.User) is Guid userId)
        {
            notifiedUserId = userId;

            if (softRestartOnSwitch && previousUserId is not null && previousUserId != userId)
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

        await AddAccount(new()
        {
            User = activeAccountProfile?.Id == userId ? activeAccountProfile : new() { Id = userId, UserName = user.Identity?.Name, Email = user.GetEmail() },
            AccessToken = accessToken,
            RefreshToken = refreshToken
        }, await storageService.IsPersistent("refresh_token"));
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

        if (account is null || GetSessionId(account.AccessToken) != GetSessionId(refreshTokenResponse.AccessToken))
            return false;

        account.AccessToken = refreshTokenResponse.AccessToken;
        account.RefreshToken = refreshTokenResponse.RefreshToken;

        await AddAccount(account, persistent);

        return false;
    }

    private async Task ForgetAccount(string? refreshToken)
    {
        if ((await GetOtherAccounts()).FirstOrDefault(account => account.RefreshToken == refreshToken) is { } account)
        {
            await RemoveAccount(account.User.Id);
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
            if ((await ReadAccounts(persistent)).FirstOrDefault(account => account.User.Id == userId) is { } account)
                return (account, persistent);
        }

        return (null, false);
    }

    private async Task AddAccount(DeviceAccountDto account, bool persistent)
    {
        await RemoveAccount(account.User.Id);

        await storageService.SetItem(AccountTokenKey(account.User.Id, "refresh_token"), account.RefreshToken, persistent);
        await storageService.SetItem(AccountTokenKey(account.User.Id, "access_token"), account.AccessToken, persistent);

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

        await storageService.RemoveItem(AccountTokenKey(userId, "access_token"));
        await storageService.RemoveItem(AccountTokenKey(userId, "refresh_token"));
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

        foreach (var account in accounts)
        {
            account.AccessToken = await storageService.GetItem(AccountTokenKey(account.User.Id, "access_token"));
            account.RefreshToken = await storageService.GetItem(AccountTokenKey(account.User.Id, "refresh_token"));
        }

        var endedAccounts = accounts.Where(account => IAuthTokenProvider.ParseAccessToken(account.RefreshToken, validateExpiry: true).IsAuthenticated() is false).ToArray();

        if (endedAccounts.Length > 0)
        {
            foreach (var account in endedAccounts)
            {
                accounts.Remove(account);
                await storageService.RemoveItem(AccountTokenKey(account.User.Id, "access_token"));
                await storageService.RemoveItem(AccountTokenKey(account.User.Id, "refresh_token"));
            }

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

        List<DeviceAccountDto> profiles = [.. accounts.Select(account => new DeviceAccountDto { User = account.User })];

        await storageService.SetItem(key, JsonSerializer.Serialize(profiles, IdentityJsonContext.Default.ListDeviceAccountDto), persistent);
    }

    private static string AccountTokenKey(Guid userId, string token) => $"account:{userId}:{token}";

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
