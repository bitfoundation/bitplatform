using System;
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
    /// <remarks>
    /// The browser's <c>document.cookie</c> API exposes only <c>name=value</c> pairs, so each
    /// returned <see cref="ButilCookie"/> has only its <see cref="ButilCookie.Name"/> and
    /// <see cref="ButilCookie.Value"/> populated. Attributes such as <see cref="ButilCookie.Domain"/>,
    /// <see cref="ButilCookie.Expires"/>, <see cref="ButilCookie.MaxAge"/>, <see cref="ButilCookie.Path"/>,
    /// <see cref="ButilCookie.SameSite"/>, <see cref="ButilCookie.Secure"/> and
    /// <see cref="ButilCookie.Partitioned"/> are never returned by the browser and will be at their
    /// default values regardless of how the cookie was originally set. <c>HttpOnly</c> cookies are not
    /// visible at all.
    /// </remarks>
    public async Task<ButilCookie[]> GetAll()
    {
        var raw = await js.InvokeFast<string>("BitButil.cookie.get");

        if (string.IsNullOrWhiteSpace(raw)) return [];

        return raw.Split(';', StringSplitOptions.RemoveEmptyEntries)
                  .Select(ButilCookie.Parse)
                  .Where(c => c is not null)
                  .Select(c => c!)
                  .ToArray();
    }

    /// <summary>
    /// Returns a cookie by providing the cookie name.
    /// </summary>
    /// <remarks>
    /// Only <see cref="ButilCookie.Name"/> and <see cref="ButilCookie.Value"/> are populated; see
    /// <see cref="GetAll"/> for why the other attributes can't be read back from the browser.
    /// </remarks>
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
        => await js.InvokeVoidFast("BitButil.cookie.set", cookie.ToString());
}
