using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The Location interface represents the location (URL) of the object it is linked to. 
/// Changes done on it are reflected on the object it relates to. 
/// Both the Document and Window interface have such a linked Location, accessible 
/// via Document.location and Window.location respectively.
/// <br />
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location">https://developer.mozilla.org/en-US/docs/Web/API/Location</see>
/// </summary>
public class Location(IJSRuntime js)
{
    /// <summary>
    /// returns a string containing the entire URL.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/href">https://developer.mozilla.org/en-US/docs/Web/API/Location/href</see>
    /// </summary>
    public async Task<string> GetHref()
        => await js.FastInvokeAsync<string>("BitButil.location.getHref");
    /// <summary>
    /// Sets the href of the location andn then the associated document navigates to the new page.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/href">https://developer.mozilla.org/en-US/docs/Web/API/Location/href</see>
    /// </summary>
    public async Task SetHref(string value)
        => await js.FastInvokeVoidAsync("BitButil.location.setHref", value);

    /// <summary>
    /// A string containing the protocol scheme of the URL, including the final ':'.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/protocol">https://developer.mozilla.org/en-US/docs/Web/API/Location/protocol</see>
    /// </summary>
    public async Task<string> GetProtocol()
        => await js.FastInvokeAsync<string>("BitButil.location.getProtocol");
    /// <summary>
    /// Sets the protocol scheme of the URL and then the associated document navigates to the new page.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/protocol">https://developer.mozilla.org/en-US/docs/Web/API/Location/protocol</see>
    /// </summary>
    public async Task SetProtocol(string value)
        => await js.FastInvokeVoidAsync("BitButil.location.setProtocol", value);

    /// <summary>
    /// A string containing the host, that is the hostname, a ':', and the port of the URL.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/host">https://developer.mozilla.org/en-US/docs/Web/API/Location/host</see>
    /// </summary>
    public async Task<string> GetHost()
        => await js.FastInvokeAsync<string>("BitButil.location.getHost");
    /// <summary>
    /// Sets the host of the location and then the associated document navigates to the new page.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/host">https://developer.mozilla.org/en-US/docs/Web/API/Location/host</see>
    /// </summary>
    public async Task SetHost(string value)
        => await js.FastInvokeVoidAsync("BitButil.location.setHost", value);

    /// <summary>
    /// A string containing the domain of the URL.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/hostname">https://developer.mozilla.org/en-US/docs/Web/API/Location/hostname</see>
    /// </summary>
    public async Task<string> GetHostname()
        => await js.FastInvokeAsync<string>("BitButil.location.getHostname");
    /// <summary>
    /// Sets the hostname of the location and then the associated document navigates to the new page.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/hostname">https://developer.mozilla.org/en-US/docs/Web/API/Location/hostname</see>
    /// </summary>
    public async Task SetHostname(string value)
        => await js.FastInvokeVoidAsync("BitButil.location.setHostname", value);

    /// <summary>
    /// A string containing the port number of the URL.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/port">https://developer.mozilla.org/en-US/docs/Web/API/Location/port</see>
    /// </summary>
    public async Task<string> GetPort()
        => await js.FastInvokeAsync<string>("BitButil.location.getPort");
    /// <summary>
    /// Sets the port of the location and then the associated document navigates to the new page.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/port">https://developer.mozilla.org/en-US/docs/Web/API/Location/port</see>
    /// </summary>
    public async Task SetPort(string value)
        => await js.FastInvokeVoidAsync("BitButil.location.setPort", value);

    /// <summary>
    /// A string containing an initial '/' followed by the path of the URL, not including the query string or fragment.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/pathname">https://developer.mozilla.org/en-US/docs/Web/API/Location/pathname</see>
    /// </summary>
    public async Task<string> GetPathname()
        => await js.FastInvokeAsync<string>("BitButil.location.getPathname");
    /// <summary>
    /// Sets the pathname of the location and then the associated document navigates to the new page.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/pathname">https://developer.mozilla.org/en-US/docs/Web/API/Location/pathname</see>
    /// </summary>
    public async Task SetPathname(string value)
        => await js.FastInvokeVoidAsync("BitButil.location.setPathname", value);

    /// <summary>
    /// A string containing a '?' followed by the parameters or "querystring" of the URL.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/search">https://developer.mozilla.org/en-US/docs/Web/API/Location/search</see>
    /// </summary>
    public async Task<string> GetSearch()
        => await js.FastInvokeAsync<string>("BitButil.location.getSearch");
    /// <summary>
    /// Sets the search of the location and then the associated document navigates to the new page.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/search">https://developer.mozilla.org/en-US/docs/Web/API/Location/search</see>
    /// </summary>
    public async Task SetSearch(string value)
        => await js.FastInvokeVoidAsync("BitButil.location.setSearch", value);

    /// <summary>
    /// A string containing a '#' followed by the fragment identifier of the URL.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/hash">https://developer.mozilla.org/en-US/docs/Web/API/Location/hash</see>
    /// </summary>
    public async Task<string> GetHash()
        => await js.FastInvokeAsync<string>("BitButil.location.getHash");
    /// <summary>
    /// Sets the hash of the location and then the associated document navigates to the new page.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/hash">https://developer.mozilla.org/en-US/docs/Web/API/Location/hash</see>
    /// </summary>
    public async Task SetHash(string value)
        => await js.FastInvokeVoidAsync("BitButil.location.setHash", value);

    /// <summary>
    /// Returns a string containing the canonical form of the origin of the specific location.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/origin">https://developer.mozilla.org/en-US/docs/Web/API/Location/origin</see>
    /// </summary>
    public async Task<string> GetOrigin()
        => await js.FastInvokeAsync<string>("BitButil.location.origin");

    /// <summary>
    /// Loads the resource at the URL provided in parameter.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/assign">https://developer.mozilla.org/en-US/docs/Web/API/Location/assign</see>
    /// </summary>
    public async Task Assign(string url)
        => await js.FastInvokeVoidAsync("BitButil.location.assign", url);

    /// <summary>
    /// Reloads the current URL, like the Refresh button.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/reload">https://developer.mozilla.org/en-US/docs/Web/API/Location/reload</see>
    /// </summary>
    public async Task Reload()
        => await js.FastInvokeVoidAsync("BitButil.location.reload");

    /// <summary>
    /// Replaces the current resource with the one at the provided URL (redirects to the provided URL).
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Location/replace">https://developer.mozilla.org/en-US/docs/Web/API/Location/replace</see>
    /// </summary>
    public async Task Replace(string url)
        => await js.FastInvokeVoidAsync("BitButil.location.replace", url);
}
