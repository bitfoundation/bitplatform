using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/BatteryManager">Battery Status API</see>.
/// </summary>
/// <remarks>
/// Browser support is uneven (Firefox/Safari intentionally don't expose this). When unsupported,
/// <see cref="IsSupported"/> returns false and <see cref="GetStatus"/> reports a charged-AC-power
/// stub so callers don't have to special-case missing data.
/// </remarks>
public class Battery(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>navigator.getBattery</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.battery.isSupported");

    /// <summary>One-shot snapshot of the battery state.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BatteryStatus))]
    public ValueTask<BatteryStatus> GetStatus() => js.Invoke<BatteryStatus>("BitButil.battery.getStatus");
}
