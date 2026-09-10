using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitChartJsRuntimeExtensions
{
    public static ValueTask<IJSObjectReference> BitChartObserve(this IJSRuntime jsRuntime,
                                                                     ElementReference element,
                                                                     DotNetObjectReference<BitChart> dotnetObj,
                                                                     bool responsive)
    {
        return jsRuntime.InvokeAsync<IJSObjectReference>("BitBlazorUI.BitChart.observe", element, dotnetObj, responsive);
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

    public static ValueTask<bool> BitChartExportSvg(this IJSRuntime jsRuntime, ElementReference element,
                                                   string fileName, string? background)
    {
        return jsRuntime.InvokeAsync<bool>("BitBlazorUI.BitChart.exportSvg", element, fileName, background);
    }

    public static ValueTask<bool> BitChartExportPng(this IJSRuntime jsRuntime, ElementReference element,
                                                   string fileName, double scale, string? background)
    {
        return jsRuntime.InvokeAsync<bool>("BitBlazorUI.BitChart.exportPng", element, fileName, scale, background);
    }

    public static ValueTask<string?> BitChartToSvgString(this IJSRuntime jsRuntime, ElementReference element, string? background)
    {
        return jsRuntime.InvokeAsync<string?>("BitBlazorUI.BitChart.toSvgString", element, background);
    }

    public static ValueTask<string?> BitChartToDataUrl(this IJSRuntime jsRuntime, ElementReference element,
                                                      string mimeType, double scale, string? background)
    {
        return jsRuntime.InvokeAsync<string?>("BitBlazorUI.BitChart.toDataUrl", element, mimeType, scale, background);
    }

    public static ValueTask BitChartDownloadText(this IJSRuntime jsRuntime, string fileName, string content, string mimeType)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.BitChart.downloadText", fileName, content, mimeType);
    }
}
