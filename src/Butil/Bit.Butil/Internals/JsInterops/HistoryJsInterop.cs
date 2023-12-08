using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class HistoryJsInterop
{
    internal static async Task<int> HistoryGetLength(this IJSRuntime js)
    {
        return await js.InvokeAsync<int>("BitButil.history.length");
    }

    internal static async Task<ScrollRestoration> HistoryGetScrollRestoration(this IJSRuntime js)
    {
        var value = await js.InvokeAsync<string>("BitButil.history.scrollRestoration");
        return value == "auto" ? ScrollRestoration.Auto : ScrollRestoration.Manual;
    }

    internal static async Task HistorySetScrollRestoration(this IJSRuntime js, ScrollRestoration value)
    {
        await js.InvokeVoidAsync("BitButil.history.setScrollRestoration", value.ToString().ToLowerInvariant());
    }

    internal static async Task<object> HistoryGetState(this IJSRuntime js)
    {
        return await js.InvokeAsync<object>("BitButil.history.state");
    }

    internal static async Task HistoryGoBack(this IJSRuntime js)
    {
        await js.InvokeVoidAsync("BitButil.history.back");
    }

    internal static async Task HistoryGoForward(this IJSRuntime js)
    {
        await js.InvokeVoidAsync("BitButil.history.forward");
    }

    internal static async Task HistoryGo(this IJSRuntime js, int? delta)
    {
        await js.InvokeVoidAsync("BitButil.history.go", delta);
    }

    internal static async Task HistoryPushState(this IJSRuntime js, object? state, string unused, string? url)
    {
        await js.InvokeVoidAsync("BitButil.history.pushState", state, unused, url);
    }

    internal static async Task HistoryReplaceState(this IJSRuntime js, object? state, string unused, string? url)
    {
        await js.InvokeVoidAsync("BitButil.history.replaceState", state, unused, url);
    }

    internal static async Task HistoryAddPopState(this IJSRuntime js, string methodName, Guid listenerId)
    {
        await js.InvokeVoidAsync("BitButil.history.addPopState", methodName, listenerId);
    }

    internal static async Task HistoryRemovePopState(this IJSRuntime js, Guid[] ids)
    {
        await js.InvokeVoidAsync("BitButil.history.removePopState", ids);
    }
}
