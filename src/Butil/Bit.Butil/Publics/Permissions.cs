using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/Permissions">navigator.permissions</see>.
/// </summary>
public class Permissions(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>navigator.permissions</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported()
        => js.Invoke<bool>("BitButil.permissions.isSupported");

    /// <summary>
    /// Returns the current state for a given permission descriptor name.
    /// </summary>
    /// <param name="name">A descriptor name such as <c>"geolocation"</c>, <c>"notifications"</c>,
    /// <c>"camera"</c>, <c>"microphone"</c>, <c>"clipboard-read"</c>, <c>"clipboard-write"</c>,
    /// <c>"push"</c>, etc. Browser support varies; unknown names return <see cref="PermissionState.Unknown"/>.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<PermissionState> Query(string name)
    {
        var raw = await js.Invoke<string>("BitButil.permissions.query", name);
        return raw switch
        {
            "granted" => PermissionState.Granted,
            "denied" => PermissionState.Denied,
            "prompt" => PermissionState.Prompt,
            _ => PermissionState.Unknown,
        };
    }
}
