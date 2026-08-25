// [mirror] IStorageService semantics - persistent vs temp storage - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Infrastructure/Services/MauiStorageService.cs
// - src/Client/Boilerplate.Client.Web/Infrastructure/Services/WebStorageService.cs
// - src/Tests/Infrastructure/Services/TestStorageService.cs
// IStorageServiceContractTests pins the behaviour all four must share.

using System.IO.IsolatedStorage;
using System.Collections.Concurrent;

namespace Boilerplate.Client.Windows.Infrastructure.Services;

public partial class WindowsStorageService : IStorageService
{
    private Dictionary<string, string?>? persistentStorage;
    private readonly ConcurrentDictionary<string, string?> tempStorage = [];

    public async ValueTask<bool> IsPersistent(string key)
    {
        persistentStorage ??= await Restore();

        return persistentStorage.ContainsKey(key);
    }

    public async ValueTask<string?> GetItem(string key)
    {
        if (tempStorage.TryGetValue(key, out string? value))
            return value;

        persistentStorage ??= await Restore();

        return persistentStorage.GetValueOrDefault(key, null);
    }

    public async ValueTask RemoveItem(string key)
    {
        tempStorage.TryRemove(key, out _);

        persistentStorage ??= await Restore();

        if (persistentStorage.Remove(key))
        {
            await Save(persistentStorage);
        }
    }

    public async ValueTask SetItem(string key, string? value, bool persistent = true)
    {
        persistentStorage ??= await Restore();

        // A key lives in exactly one of the two stores. Writing to one without removing it from the other would leave
        // the previous value where GetItem still reads it - and since GetItem reads tempStorage first, a persistent
        // write would be shadowed by the temporary value it supersedes.
        if (persistent)
        {
            tempStorage.TryRemove(key, out _);
            persistentStorage[key] = value;
            await Save(persistentStorage);
        }
        else
        {
            tempStorage[key] = value;
            if (persistentStorage.Remove(key))
            {
                await Save(persistentStorage);
            }
        }
    }

    public async ValueTask Clear()
    {
        persistentStorage?.Clear();
        tempStorage?.Clear();
        await Save([]);
    }

    const string WindowsStorageFilename = "Boilerplate.Client.Windows.storage.json";
    private static readonly SemaphoreSlim ioLock = new(1, 1);
    // Restore application-scope property from isolated storage
    private static async Task<Dictionary<string, string?>> Restore()
    {
        try
        {
            await ioLock.WaitAsync();
            using IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForDomain();
            using IsolatedStorageFileStream stream = new IsolatedStorageFileStream(WindowsStorageFilename, FileMode.OpenOrCreate, storage);
            if (stream.Length == 0)
                return [];
            return (await JsonSerializer.DeserializeAsync(stream, AppJsonContext.Default.DictionaryStringString))!;
        }
        finally
        {
            ioLock.Release();
        }
    }

    // Persist application-scope property to isolated storage
    private static async Task Save(Dictionary<string, string?> data)
    {
        try
        {
            await ioLock.WaitAsync();
            using IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForDomain();
            using IsolatedStorageFileStream stream = new IsolatedStorageFileStream(WindowsStorageFilename, FileMode.Create, storage);
            using StreamWriter writer = new StreamWriter(stream);
            await writer.WriteAsync(JsonSerializer.Serialize(data, AppJsonContext.Default.DictionaryStringString));
        }
        finally
        {
            ioLock.Release();
        }
    }
}
