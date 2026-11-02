// [mirror] IStorageService semantics - persistent vs temp storage - keep in sync with:
// - src/Client/Boilerplate.Client.Windows/Infrastructure/Services/WindowsStorageService.cs
// - src/Client/Boilerplate.Client.Web/Infrastructure/Services/WebStorageService.cs
// - src/Tests/Infrastructure/Services/TestStorageService.cs
// - src/Workbench/Infrastructure/Services/StandIns/WorkbenchStorageService.cs
// IStorageServiceContractTests pins the behaviour all four must share.

using System.Collections.Concurrent;

namespace Boilerplate.Client.Maui.Infrastructure.Services;

public partial class MauiStorageService : IStorageService
{
    private const string TokenStorageInitializedKey = "TokenStorageInitialized";

    private readonly ConcurrentDictionary<string, string?> tempStorage = [];

    private readonly ConcurrentDictionary<string, string?> tokenStorage = [];

    private readonly Lazy<bool> tokenStorageInitialization = new(InitializeTokenStorage);

    public async ValueTask<string?> GetItem(string key)
    {
        if (IAuthTokenProvider.IsTokenStorageKey(key))
        {
            return await GetToken(key) ?? tempStorage.GetValueOrDefault(key);
        }

        tempStorage.TryGetValue(key, out string? value);
        return Preferences.Get(key, value);
    }

    public async ValueTask<bool> IsPersistent(string key)
    {
        if (IAuthTokenProvider.IsTokenStorageKey(key))
        {
            return await GetToken(key) is not null;
        }

        return Preferences.ContainsKey(key);
    }

    public async ValueTask RemoveItem(string key)
    {
        if (IAuthTokenProvider.IsTokenStorageKey(key))
        {
            _ = tokenStorageInitialization.Value;
            RemoveToken(key);
        }

        Preferences.Remove(key);
        tempStorage.TryRemove(key, out _);
    }

    public async ValueTask SetItem(string key, string? value, bool persistent = true)
    {
        var isToken = IAuthTokenProvider.IsTokenStorageKey(key);

        if (isToken)
        {
            _ = tokenStorageInitialization.Value;
        }

        // A key lives in exactly one of the two stores. Writing to one without removing it from the other would leave
        // the previous value where GetItem still reads it - and since Preferences wins there (it is the value, the temp
        // entry is only the default), a temporary write would be shadowed by the persistent value it supersedes.
        if (persistent)
        {
            tempStorage.TryRemove(key, out _);

            if (isToken)
            {
                await SetToken(key, value);
            }
            else
            {
                Preferences.Set(key, value);
            }
        }
        else
        {
            if (isToken)
            {
                RemoveToken(key);
            }
            else
            {
                Preferences.Remove(key);
            }

            tempStorage[key] = value;
        }
    }

    public async ValueTask Clear()
    {
        _ = tokenStorageInitialization.Value;

        tempStorage.Clear();
        tokenStorage.Clear();
        Preferences.Clear();
        Preferences.Set(TokenStorageInitializedKey, true);
        TryRemoveAllSecureItems();
    }

    private async Task<string?> GetToken(string key)
    {
        _ = tokenStorageInitialization.Value;

        if (tokenStorage.TryGetValue(key, out var token))
            return token;

        try
        {
            token = await SecureStorage.GetAsync(key);
        }
        catch (Exception exp)
        {
            IPlatformApplication.Current?.Services.GetService<ILogger<MauiStorageService>>()?
                .LogWarning(exp, "{Key} could not be read from secure storage.", key);
            return null;
        }

        return tokenStorage.GetOrAdd(key, token);
    }

    private async Task SetToken(string key, string? value)
    {
        if (value is null)
        {
            RemoveToken(key);
            return;
        }

        try
        {
            await SecureStorage.SetAsync(key, value);
            tokenStorage[key] = value;
        }
        catch (Exception exp)
        {
            RemoveToken(key);
            tempStorage[key] = value;
            IPlatformApplication.Current?.Services.GetService<ILogger<MauiStorageService>>()?
                .LogError(exp, "{Key} could not be written to secure storage, so it is kept until the app closes.", key);
        }
    }

    private void RemoveToken(string key)
    {
        tokenStorage[key] = null;
        TryRemoveSecureItem(key);
    }

    private static bool InitializeTokenStorage()
    {
        if (Preferences.ContainsKey(TokenStorageInitializedKey) is false)
        {
            TryRemoveAllSecureItems();
            Preferences.Set(TokenStorageInitializedKey, true);
        }

        return true;
    }

    private static void TryRemoveSecureItem(string key)
    {
        try
        {
            SecureStorage.Remove(key);
        }
        catch { }
    }

    private static void TryRemoveAllSecureItems()
    {
        try
        {
            SecureStorage.RemoveAll();
        }
        catch { }
    }
}
