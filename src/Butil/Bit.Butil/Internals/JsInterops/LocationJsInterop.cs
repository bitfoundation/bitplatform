using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class LocationJsInterop
{
    internal static async Task<string> LocationGetHref(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.location.getHref");
    internal static async Task LocationSetHref(this IJSRuntime js, string value)
        => await js.InvokeVoidAsync("BitButil.location.setHref", value);

    internal static async Task<string> LocationGetProtocol(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.location.getProtocol");
    internal static async Task LocationSetProtocol(this IJSRuntime js, string value)
        => await js.InvokeVoidAsync("BitButil.location.setProtocol", value);

    internal static async Task<string> LocationGetHost(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.location.getHost");
    internal static async Task LocationSetHost(this IJSRuntime js, string value)
        => await js.InvokeVoidAsync("BitButil.location.setHost", value);

    internal static async Task<string> LocationGetHostname(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.location.getHostname");
    internal static async Task LocationSetHostname(this IJSRuntime js, string value)
        => await js.InvokeVoidAsync("BitButil.location.setHostname", value);

    internal static async Task<string> LocationGetPort(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.location.getPort");
    internal static async Task LocationSetPort(this IJSRuntime js, string value)
        => await js.InvokeVoidAsync("BitButil.location.setPort", value);

    internal static async Task<string> LocationGetPathname(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.location.getPathname");
    internal static async Task LocationSetPathname(this IJSRuntime js, string value)
        => await js.InvokeVoidAsync("BitButil.location.setPathname", value);

    internal static async Task<string> LocationGetSearch(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.location.getSearch");
    internal static async Task LocationSetSearch(this IJSRuntime js, string value)
        => await js.InvokeVoidAsync("BitButil.location.setSearch", value);

    internal static async Task<string> LocationGetHash(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.location.getHash");
    internal static async Task LocationSetHash(this IJSRuntime js, string value)
        => await js.InvokeVoidAsync("BitButil.location.setHash", value);

    internal static async Task<string> LocationGetOrigin(this IJSRuntime js)
        => await js.InvokeAsync<string>("BitButil.location.origin");

    internal static async Task LocationAssign(this IJSRuntime js, string url)
        => await js.InvokeVoidAsync("BitButil.location.assign", url);

    internal static async Task LocationReload(this IJSRuntime js)
        => await js.InvokeVoidAsync("BitButil.location.reload");

    internal static async Task LocationReplace(this IJSRuntime js, string url)
        => await js.InvokeVoidAsync("BitButil.location.replace", url);
}
