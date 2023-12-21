using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class CookieJsInterop
{
    internal static async Task<ButilCookie[]> CookieGetAll(this IJSRuntime js)
    {
        var cookie = await js.InvokeAsync<string>("BitButil.cookie.get");
        return cookie.Split(';').Select(ButilCookie.Parse).ToArray();
    }

    internal static async Task<ButilCookie?> CookieGet(this IJSRuntime js, string name)
    {
        var allCookies = await CookieGetAll(js);
        return allCookies.FirstOrDefault(c => c.Name == name);
    }

    internal static async Task CookieRemove(this IJSRuntime js, string name)
    {
        var cookie = new ButilCookie { Name = name, MaxAge = 0, Expires = null };
        await CookieSet(js, cookie);
    }

    internal static async Task CookieRemove(this IJSRuntime js, ButilCookie cookie)
    {
        cookie.MaxAge = 0;
        cookie.Expires = null;
        await CookieSet(js, cookie);
    }

    internal static async Task CookieSet(this IJSRuntime js, ButilCookie cookie)
        => await js.InvokeVoidAsync("BitButil.cookie.set", cookie.ToString());
}
