namespace Boilerplate.Client.Core.Services;

public partial class LocalStorageService : IStorageService
{
    [AutoInject] private IJSRuntime jsRuntime;

    public async ValueTask<string?> GetItem(string key)
    {
        return await jsRuntime.InvokeAsync<string?>("window.localStorage.getItem", key) ??
            await jsRuntime.InvokeAsync<string?>("window.sessionStorage.getItem", key);
    }

    public async ValueTask RemoveItem(string key)
    {
        await jsRuntime.InvokeAsync<string?>("window.localStorage.removeItem", key);
        await jsRuntime.InvokeAsync<string?>("window.sessionStorage.removeItem", key);
    }

    public async ValueTask SetItem(string key, string? value, bool persistent = true)
    {
        await jsRuntime.InvokeAsync<string?>($"window.{(persistent ? "localStorage" : "sessionStorage")}.setItem", key, value);
    }

    public async ValueTask<bool> IsPersistent(string key)
    {
        return (await jsRuntime.InvokeAsync<string?>("window.localStorage.getItem", key)) is not null;
    }
}
