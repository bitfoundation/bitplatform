using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/Permissions">navigator.permissions</see>.
/// </summary>
public class Permissions(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>navigator.permissions</c>.</summary>
    public async ValueTask<bool> IsSupported()
        => await js.Invoke<bool>("BitButil.permissions.isSupported");

    /// <summary>
    /// Returns the current state for a given permission descriptor name.
    /// </summary>
    /// <param name="name">A descriptor name such as <c>"geolocation"</c>, <c>"notifications"</c>,
    /// <c>"camera"</c>, <c>"microphone"</c>, <c>"clipboard-read"</c>, <c>"clipboard-write"</c>,
    /// <c>"push"</c>, etc. Browser support varies; unknown names return <see cref="PermissionState.Unknown"/>.</param>
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
