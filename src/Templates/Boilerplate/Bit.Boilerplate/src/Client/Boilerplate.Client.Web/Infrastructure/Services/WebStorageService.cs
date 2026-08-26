// [mirror] IStorageService semantics - persistent vs temp storage - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Infrastructure/Services/MauiStorageService.cs
// - src/Client/Boilerplate.Client.Windows/Infrastructure/Services/WindowsStorageService.cs
// - src/Tests/Infrastructure/Services/TestStorageService.cs
// IStorageServiceContractTests pins the behaviour all four must share.

using Bit.Butil;

namespace Boilerplate.Client.Web.Infrastructure.Services;

public partial class WebStorageService : IStorageService
{
    [AutoInject] private SessionStorage sessionStorage;
    [AutoInject] private LocalStorage localStorage;

    public async ValueTask<string?> GetItem(string key)
    {
        try
        {
            BitButil.UseFastInvoke(); // In Blazor WebAssembly, `IJSInProcessRuntime` is used for `localStorage` and `sessionStorage`, providing a slight performance boost.

            return await localStorage.GetItem(key) ??
                await sessionStorage.GetItem(key);
        }
        finally
        {
            BitButil.UseNormalInvoke();
        }
    }

    public async ValueTask RemoveItem(string key)
    {
        await localStorage.RemoveItem(key);
        await sessionStorage.RemoveItem(key);
    }

    public async ValueTask SetItem(string key, string? value, bool persistent = true)
    {
        if (value is null)
        {
            // Web Storage stringifies whatever it is handed, so a null crosses JS interop and comes back out of
            // GetItem as the four characters "null" - a value that is neither null nor empty and therefore passes
            // every emptiness guard in the app. Removing the key is what MauiStorageService already does, through
            // Preferences.Set(key, null).
            await RemoveItem(key);
            return;
        }

        // A key lives in exactly one of the two stores. Writing to one without removing it from the other would leave
        // the previous value where GetItem still reads it - and since GetItem reads localStorage first, a temporary
        // write would be shadowed by the persistent value it supersedes.
        if (persistent)
        {
            await sessionStorage.RemoveItem(key);
            await localStorage.SetItem(key, value);
        }
        else
        {
            await localStorage.RemoveItem(key);
            await sessionStorage.SetItem(key, value);
        }
    }

    public async ValueTask<bool> IsPersistent(string key)
    {
        return (await localStorage.GetItem(key)) is not null;
    }

    public async ValueTask Clear()
    {
        await localStorage.Clear();
        await sessionStorage.Clear();
    }
}
