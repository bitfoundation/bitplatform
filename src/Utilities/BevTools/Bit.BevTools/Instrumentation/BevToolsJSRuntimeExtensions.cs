using Microsoft.JSInterop;

namespace Bit.BevTools;

public static class BevToolsJSRuntimeExtensions
{
    /// <summary>
    /// Returns a runtime that records calls in the BevTools timeline. Use in services that make JS calls; components
    /// are wrapped automatically. Returns the original runtime when BevTools is disabled.
    /// </summary>
    public static IJSRuntime WithBevToolsTracking(this IJSRuntime jsRuntime, BevToolsSession session)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(session);
        return session.IsEnabled ? BevToolsTrackingJSRuntime.Create(jsRuntime, session, null) : jsRuntime;
    }
}
