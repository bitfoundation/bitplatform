using Microsoft.JSInterop;

namespace Bit.BlazeDb.Demo.Client.Components;

/// <summary>Thin wrapper over the demo's own ES module in <c>wwwroot/js/app.js</c>.</summary>
public sealed class AppInterop(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    private async ValueTask<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/app.js");

    public async ValueTask<string> GetThemeAsync()
    {
        try
        {
            var module = await ModuleAsync();
            return await module.InvokeAsync<string>("getTheme");
        }
        catch (JSException)
        {
            return "dark";
        }
    }

    public async ValueTask SetThemeAsync(string theme)
    {
        try
        {
            var module = await ModuleAsync();
            await module.InvokeVoidAsync("setTheme", theme);
        }
        catch (JSException)
        {
            // A missing module must never break navigation.
        }
    }

    public async ValueTask<bool> CopyAsync(string text)
    {
        try
        {
            var module = await ModuleAsync();
            return await module.InvokeAsync<bool>("copyText", text);
        }
        catch (JSException)
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Nothing to release.
            }
            _module = null;
        }
    }
}
