// [mirror] IStorageService semantics - persistent vs temp storage - keep in sync with:
// - src/Client/Boilerplate.Client.Windows/Infrastructure/Services/WindowsStorageService.cs
// - src/Client/Boilerplate.Client.Web/Infrastructure/Services/WebStorageService.cs
// - src/Tests/Infrastructure/Services/TestStorageService.cs
// IStorageServiceContractTests pins the behaviour all four must share.

using System.Collections.Concurrent;

namespace Boilerplate.Client.Maui.Infrastructure.Services;

public partial class MauiStorageService : IStorageService
{
    private readonly ConcurrentDictionary<string, string?> tempStorage = [];

    public async ValueTask<string?> GetItem(string key)
    {
        tempStorage.TryGetValue(key, out string? value);
        return Preferences.Get(key, value);
    }

    public async ValueTask<bool> IsPersistent(string key)
    {
        return Preferences.ContainsKey(key);
    }

    public async ValueTask RemoveItem(string key)
    {
        Preferences.Remove(key);
        tempStorage.TryRemove(key, out _);
    }

    public async ValueTask SetItem(string key, string? value, bool persistent = true)
    {
        // A key lives in exactly one of the two stores. Writing to one without removing it from the other would leave
        // the previous value where GetItem still reads it - and since Preferences wins there (it is the value, the temp
        // entry is only the default), a temporary write would be shadowed by the persistent value it supersedes.
        if (persistent)
        {
            tempStorage.TryRemove(key, out _);
            Preferences.Set(key, value);
        }
        else
        {
            Preferences.Remove(key);
            tempStorage[key] = value;
        }
    }

    public async ValueTask Clear()
    {
        tempStorage.Clear();
        Preferences.Clear();
    }
}
