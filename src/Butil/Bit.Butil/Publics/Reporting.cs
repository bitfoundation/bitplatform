using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Reporting_API">Reporting API</see>
/// (<c>ReportingObserver</c>).
/// </summary>
/// <remarks>
/// Useful for surfacing browser-emitted deprecation, intervention, CSP-violation, and crash
/// reports to your monitoring stack alongside ordinary errors.
/// </remarks>
public class Reporting(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>ReportingObserver</c>.</summary>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.reporting.isSupported");

    /// <summary>
    /// Subscribes to browser-generated reports. Use the returned <see cref="ButilSubscription"/> to stop.
    /// </summary>
    /// <param name="types">Optional whitelist of report types (e.g. <c>"deprecation"</c>, <c>"intervention"</c>).
    /// Pass null to receive every type.</param>
    /// <param name="buffered">When true, also delivers reports queued before the observer registered.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BrowserReport))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ReportingListenersManager))]
    public async Task<ButilSubscription> Subscribe(Action<BrowserReport[]> handler,
                                                   string[]? types = null,
                                                   bool buffered = true)
    {
        var id = ReportingListenersManager.AddListener(handler);
        await js.InvokeVoid("BitButil.reporting.observe",
            ReportingListenersManager.InvokeMethodName,
            id,
            types,
            buffered);

        return new ButilSubscription(id, async () =>
        {
            ReportingListenersManager.RemoveListener(id);
            if (OperatingSystem.IsBrowser() is false) return;
            await js.InvokeVoid("BitButil.reporting.disconnect", id);
        });
    }
}
