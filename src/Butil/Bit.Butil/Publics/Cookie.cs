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
        => await js.CookieGetAll();

    /// <summary>
    /// Returns a cookie by providing the cookie name.
    /// </summary>
    public async Task<ButilCookie?> Get(string name)
        => await js.CookieGet(name);

    /// <summary>
    /// Removes a cookie by providing the its name.
    /// </summary>
    public async Task Remove(string name)
        => await js.CookieRemove(name);

    /// <summary>
    /// Removes a cookie.
    /// </summary>
    public async Task Remove(ButilCookie cookie)
        => await js.CookieRemove(cookie);

    /// <summary>
    /// Sets a cookie.
    /// </summary>
    public async Task Set(ButilCookie cookie)
        => await js.CookieSet(cookie);
}
