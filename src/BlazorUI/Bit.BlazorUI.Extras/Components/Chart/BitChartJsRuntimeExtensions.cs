using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitChartJsRuntimeExtensions
{
    public static ValueTask<IJSObjectReference> BitChartObserve(this IJSRuntime jsRuntime,
                                                                     ElementReference element,
                                                                     DotNetObjectReference<BitChart> dotnetObj)
    {
        return jsRuntime.InvokeAsync<IJSObjectReference>("BitBlazorUI.BitChart.observe", element, dotnetObj);
    }

    // The zoom payload is only ever constructed (never read) from C#, so without this hint the
    // trimmer strips its property getters and the reflection-based interop serialization silently
    // sends an empty object to the bridge in trimmed (release) builds.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitChartZoomPayload))]
    public static ValueTask<IJSObjectReference> BitChartRegister(this IJSRuntime jsRuntime,
                                                                      ElementReference element,
                                                                      DotNetObjectReference<BitChart> dotnetObj,
                                                                      BitChartZoomPayload options)
    {
        return jsRuntime.InvokeAsync<IJSObjectReference>("BitBlazorUI.BitChart.register", element, dotnetObj, options);
    }
}
