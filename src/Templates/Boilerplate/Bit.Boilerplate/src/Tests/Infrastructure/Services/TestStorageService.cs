// [mirror] IStorageService semantics - persistent vs temp storage - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Infrastructure/Services/MauiStorageService.cs
// - src/Client/Boilerplate.Client.Windows/Infrastructure/Services/WindowsStorageService.cs
// - src/Client/Boilerplate.Client.Web/Infrastructure/Services/WebStorageService.cs
// - src/Workbench/Infrastructure/Services/StandIns/WorkbenchStorageService.cs
// IStorageServiceContractTests pins the behaviour all four must share.

namespace Boilerplate.Tests.Infrastructure.Services;

internal partial class TestStorageService : IStorageService
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
