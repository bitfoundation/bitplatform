namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

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
