// [mirror] IStorageService semantics - persistent vs temp storage - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Infrastructure/Services/MauiStorageService.cs
// - src/Client/Boilerplate.Client.Windows/Infrastructure/Services/WindowsStorageService.cs
// - src/Client/Boilerplate.Client.Web/Infrastructure/Services/WebStorageService.cs
// IStorageServiceContractTests pins the behaviour all four must share.

namespace Boilerplate.Tests.Infrastructure.Services;

/// <summary>
/// In UI tests, browser will uses its own storage, but for api tests, we need to fake the storage.
/// <para>
/// It models the two stores the shipped implementations have, because <c>AuthManager.StoreTokens</c> asks
/// <see cref="IsPersistent"/> which of them a key is in and decides "remember me" from the answer. A single-store fake
/// that hard-coded <c>IsPersistent =&gt; false</c> made that line a constant under test, so no test could reach the
/// contract the shipped clients have to honour.
/// </para>
/// </summary>
public partial class TestStorageService : IStorageService
{
    private readonly Dictionary<string, string?> tempStorage = [];
    private readonly Dictionary<string, string?> persistentStorage = [];

    public async ValueTask<string?> GetItem(string key)
    {
        if (persistentStorage.TryGetValue(key, out string? persistentValue))
            return persistentValue;

        tempStorage.TryGetValue(key, out string? tempValue);
        return tempValue;
    }

    public async ValueTask<bool> IsPersistent(string key)
    {
        return persistentStorage.ContainsKey(key);
    }

    public async ValueTask RemoveItem(string key)
    {
        tempStorage.Remove(key);
        persistentStorage.Remove(key);
    }

    public async ValueTask SetItem(string key, string? value, bool persistent = true)
    {
        if (value is null)
        {
            // Storing a null must remove the key (see WebStorageService/MauiStorageService), otherwise IsPersistent
            // would keep answering true for a value that no longer exists - the axis AuthManager derives "remember me" from.
            await RemoveItem(key);
            return;
        }

        // A key lives in exactly one of the two stores. Writing to one without removing it from the other would leave
        // the previous value where GetItem still reads it - and since Preferences wins there (it is the value, the temp
        // entry is only the default), a temporary write would be shadowed by the persistent value it supersedes.
        if (persistent)
        {
            tempStorage.Remove(key);
            persistentStorage[key] = value;
        }
        else
        {
            persistentStorage.Remove(key);
            tempStorage[key] = value;
        }
    }

    public async ValueTask Clear()
    {
        tempStorage.Clear();
        persistentStorage.Clear();
    }
}
