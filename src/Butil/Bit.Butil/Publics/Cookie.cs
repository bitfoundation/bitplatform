using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The Document property cookie lets you read and write cookies associated with the document. 
/// It serves as a getter and setter for the actual values of the cookies.
/// <br />
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/cookie">https://developer.mozilla.org/en-US/docs/Web/API/Document/cookie</see>
/// </summary>
public class Cookie(IJSRuntime js)
{
    /// <summary>
    /// Gets all cookies registered on the current document.
    /// </summary>
    public async Task<ButilCookie[]> GetAll()
    {
        var cookie = await js.FastInvokeAsync<string>("BitButil.cookie.get");
        return cookie.Split(';').Select(ButilCookie.Parse).ToArray();
    }

    /// <summary>
    /// Returns a cookie by providing the cookie name.
    /// </summary>
    public async Task<ButilCookie?> Get(string name)
    {
        var allCookies = await GetAll();
        return allCookies.FirstOrDefault(c => c.Name == name);
    }

    /// <summary>
    /// Returns the cookie value by providing its name.
    /// </summary>
    public async Task<string?> GetValue(string name)
    {
        var allCookies = await GetAll();
        return allCookies.FirstOrDefault(c => c.Name == name)?.Value;
    }

    /// <summary>
    /// Removes a cookie by providing the its name.
    /// </summary>
    public async Task Remove(string name)
    {
        var cookie = new ButilCookie { Name = name, MaxAge = 0, Expires = null };
        await Set(cookie);
    }

    /// <summary>
    /// Removes a cookie.
    /// </summary>
    public async Task Remove(ButilCookie cookie)
    {
        cookie.MaxAge = 0;
        cookie.Expires = null;
        await Set(cookie);
    }

    /// <summary>
    /// Sets a cookie.
    /// </summary>
    public async Task Set(ButilCookie cookie)
        => await js.FastInvokeVoidAsync("BitButil.cookie.set", cookie.ToString());
}
