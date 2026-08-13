using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/NetworkInformation">Network Information API</see>
/// (<c>navigator.connection</c>) plus the always-available <c>navigator.onLine</c>.
/// </summary>
public class NetworkInformation(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>navigator.connection</c>.</summary>
    /// <remarks>
    /// This is the Chromium-only half of the API. <see cref="GetStatus"/> works regardless - on a
    /// browser that answers <c>false</c> here it still reports
    /// <see cref="NetworkConnectionStatus.Online"/> from <c>navigator.onLine</c> and leaves the
    /// connection-quality fields null - so treat this as "is the detail available", not as
    /// "may I call GetStatus".
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.networkInformation.isSupported");

    /// <summary>One-shot snapshot of the network state.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NetworkConnectionStatus))]
    public ValueTask<NetworkConnectionStatus> GetStatus()
        => js.Invoke<NetworkConnectionStatus>("BitButil.networkInformation.getStatus");
}
