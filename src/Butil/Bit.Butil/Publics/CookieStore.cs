using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the modern async <see href="https://developer.mozilla.org/en-US/docs/Web/API/CookieStore">CookieStore</see> API.
/// </summary>
/// <remarks>
/// The legacy <see cref="Cookie"/> service still works on every browser, but it can only see Name/Value
/// because <c>document.cookie</c> doesn't expose other attributes. Use this service when you need the full
/// metadata (Domain/Path/Expires/SameSite). Browser support is Chromium-only at the time of writing.
/// </remarks>
public class CookieStore(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>cookieStore</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.cookieStore.isSupported");

    /// <summary>Returns every cookie visible to the current document.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CookieStoreItem))]
    public ValueTask<CookieStoreItem[]> GetAll() => js.Invoke<CookieStoreItem[]>("BitButil.cookieStore.getAll");

    /// <summary>Returns the cookie with the given name, or null when absent.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CookieStoreItem))]
    public ValueTask<CookieStoreItem?> Get(string name) => js.Invoke<CookieStoreItem?>("BitButil.cookieStore.get", name);

    /// <summary>Sets a cookie. Use <see cref="Delete"/> to remove one (don't pass MaxAge=0 - that's the legacy trick).</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CookieStoreItem))]
    public ValueTask Set(CookieStoreItem cookie) => js.InvokeVoid("BitButil.cookieStore.set", cookie);

    /// <summary>Deletes the named cookie.</summary>
    public ValueTask Delete(string name) => js.InvokeVoid("BitButil.cookieStore.delete", name);
}
