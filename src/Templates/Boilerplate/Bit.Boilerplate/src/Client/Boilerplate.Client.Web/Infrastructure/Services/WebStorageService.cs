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
        if (persistent)
        {
            await localStorage.SetItem(key, value);
        }
        else
        {
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
