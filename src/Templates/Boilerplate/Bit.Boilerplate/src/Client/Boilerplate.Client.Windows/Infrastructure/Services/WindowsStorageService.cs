// [mirror] IStorageService semantics - persistent vs temp storage - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Infrastructure/Services/MauiStorageService.cs
// - src/Client/Boilerplate.Client.Web/Infrastructure/Services/WebStorageService.cs
// - src/Tests/Infrastructure/Services/TestStorageService.cs
// IStorageServiceContractTests pins the behaviour all four must share.

using System.IO.IsolatedStorage;
using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Windows.Infrastructure.Services;

public partial class WindowsStorageService : IStorageService
{
    private ConcurrentDictionary<string, string?>? persistentStorage;
    private readonly ConcurrentDictionary<string, string?> tempStorage = [];

    public async ValueTask<bool> IsPersistent(string key)
    {
        var storage = await GetPersistentStorage();

        return storage.ContainsKey(key);
    }

    public async ValueTask<string?> GetItem(string key)
    {
        if (tempStorage.TryGetValue(key, out string? value))
            return value;

        var storage = await GetPersistentStorage();

        return storage.GetValueOrDefault(key, null);
    }

    public async ValueTask RemoveItem(string key)
    {
        tempStorage.TryRemove(key, out _);

        var storage = await GetPersistentStorage();

        if (storage.TryRemove(key, out _))
        {
            await Save(storage);
        }
    }

    public async ValueTask SetItem(string key, string? value, bool persistent = true)
    {
        var storage = await GetPersistentStorage();

        // A key lives in exactly one of the two stores. Writing to one without removing it from the other would leave
        // the previous value where GetItem still reads it - and since GetItem reads tempStorage first, a persistent
        // write would be shadowed by the temporary value it supersedes.
        if (persistent)
        {
            tempStorage.TryRemove(key, out _);
            storage[key] = value;
            await Save(storage);
        }
        else
        {
            tempStorage[key] = value;
            if (storage.TryRemove(key, out _))
            {
                await Save(storage);
            }
        }
    }

    public async ValueTask Clear()
    {
        persistentStorage?.Clear();
        tempStorage?.Clear();
        await Save([]);
    }

    private async ValueTask<ConcurrentDictionary<string, string?>> GetPersistentStorage()
    {
        if (persistentStorage is not null)
            return persistentStorage;

        var restored = await Restore();

        return Interlocked.CompareExchange(ref persistentStorage, restored, null) ?? restored;
    }

    const string WindowsStorageFilename = "Boilerplate.Client.Windows.storage.json";
    private static readonly SemaphoreSlim ioLock = new(1, 1);
    // Restore application-scope property from isolated storage
    private static async Task<ConcurrentDictionary<string, string?>> Restore()
    {
        try
        {
            await ioLock.WaitAsync();
            using IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForDomain();
            using IsolatedStorageFileStream stream = new IsolatedStorageFileStream(WindowsStorageFilename, FileMode.OpenOrCreate, storage);
            if (stream.Length == 0)
                return [];
            var restored = await JsonSerializer.DeserializeAsync(stream, AppJsonContext.Default.DictionaryStringString);
            return restored is null ? [] : new(restored);
        }
        catch (Exception exp) when (exp is JsonException or IsolatedStorageException or IOException)
        {
            // Save() truncates the file at open, so a process killed mid-write leaves it empty (handled above) or, for
            // a payload past the stream buffer, cut short. Without this the throw escapes the synchronous
            // GetItem("Culture") in Program.Main and the app fails to start on every subsequent launch, with no way to
            // clear the store from inside the app it prevents from opening. Losing the contents costs a sign-in.
            TryQuarantineCorruptStore(exp);
            return [];
        }
        finally
        {
            ioLock.Release();
        }
    }

    /// <summary>
    /// Moves the unreadable store aside instead of deleting it, so it is still there to look at, and so the next
    /// Save() is not writing over a file we could not parse. Best effort by construction: it runs on a path that is
    /// already handling a failed read, and nothing it does may throw.
    /// </summary>
    private static void TryQuarantineCorruptStore(Exception exp)
    {
        // Separate from the move below, and both best effort: this runs while a read has already failed, and an
        // exception escaping here would take the recovery down with it. Program.Services is assigned before the first
        // Restore(), so the logger is available - and without this line the user silently loses their refresh token
        // and culture with nothing anywhere recording why.
        try
        {
            Program.Services?.GetService<ILogger<WindowsStorageService>>()?
                   .LogError(exp, "{File} could not be read and was quarantined; its contents are lost.", WindowsStorageFilename);
        }
        catch { }

        try
        {
            using IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForDomain();
            var quarantinedName = $"{WindowsStorageFilename}.corrupt";
            if (storage.FileExists(quarantinedName))
            {
                storage.DeleteFile(quarantinedName);
            }
            storage.MoveFile(WindowsStorageFilename, quarantinedName);
        }
        catch { }
    }

    // Persist application-scope property to isolated storage
    private static async Task Save(ConcurrentDictionary<string, string?> data)
    {
        // A snapshot, not the live store: JsonSerializer enumerates what it is given, and another thread may be adding
        // a key to `data` while this runs.
        var snapshot = new Dictionary<string, string?>(data);

        try
        {
            await ioLock.WaitAsync();
            using IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForDomain();
            using IsolatedStorageFileStream stream = new IsolatedStorageFileStream(WindowsStorageFilename, FileMode.Create, storage);
            using StreamWriter writer = new StreamWriter(stream);
            await writer.WriteAsync(JsonSerializer.Serialize(snapshot, AppJsonContext.Default.DictionaryStringString));
        }
        finally
        {
            ioLock.Release();
        }
    }
}
