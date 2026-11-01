// [mirror] IStorageService semantics - persistent vs temp storage - keep in sync with:
// - src/Client/Boilerplate.Client.Web/Infrastructure/Services/WebStorageService.cs
// - src/Client/Boilerplate.Client.Maui/Infrastructure/Services/MauiStorageService.cs
// - src/Client/Boilerplate.Client.Windows/Infrastructure/Services/WindowsStorageService.cs
// - src/Tests/Infrastructure/Services/TestStorageService.cs

namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

/// <summary>
/// The app's storage service on the browser's localStorage for persistent items and sessionStorage for the rest.
/// </summary>
public partial class WorkbenchStorageService : IStorageService
{
    [AutoInject] private SessionStorage sessionStorage = default!;
    [AutoInject] private LocalStorage localStorage = default!;

    public async ValueTask<string?> GetItem(string key)
    {
        return await localStorage.GetItem(key) ?? await sessionStorage.GetItem(key);
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
            await RemoveItem(key);
            return;
        }

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
