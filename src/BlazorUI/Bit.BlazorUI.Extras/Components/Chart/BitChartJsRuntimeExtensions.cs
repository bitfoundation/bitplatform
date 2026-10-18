using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bit.BlazorUI;

internal static class BitChartJsRuntimeExtensions
{
    public static ValueTask<IJSObjectReference> BitChartObserve(this IJSRuntime jsRuntime,
                                                                     ElementReference element,
                                                                     DotNetObjectReference<BitChart> dotnetObj,
                                                                     bool responsive)
    {
        return jsRuntime.InvokeScriptAsync<IJSObjectReference>("BitBlazorUI.BitChart.observe", element, dotnetObj, responsive);
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
        return jsRuntime.InvokeScriptAsync<IJSObjectReference>("BitBlazorUI.BitChart.register", element, dotnetObj, options);
    }

    public static ValueTask<bool> BitChartExportSvg(this IJSRuntime jsRuntime, ElementReference element,
                                                   string fileName, string? background)
    {
        return jsRuntime.InvokeScriptAsync<bool>("BitBlazorUI.BitChart.exportSvg", element, fileName, background);
    }

    public static ValueTask<bool> BitChartExportPng(this IJSRuntime jsRuntime, ElementReference element,
                                                   string fileName, double scale, string? background)
    {
        return jsRuntime.InvokeScriptAsync<bool>("BitBlazorUI.BitChart.exportPng", element, fileName, scale, background);
    }

    public static async ValueTask<string?> BitChartToSvgString(this IJSRuntime jsRuntime, ElementReference element, string? background)
    {
        return await ReadText(await jsRuntime.InvokeScriptAsync<IJSStreamReference?>("BitBlazorUI.BitChart.toSvgString", element, background));
    }

    public static async ValueTask<string?> BitChartToDataUrl(this IJSRuntime jsRuntime, ElementReference element,
                                                            string mimeType, double scale, string? background)
    {
        return await ReadText(await jsRuntime.InvokeScriptAsync<IJSStreamReference?>("BitBlazorUI.BitChart.toDataUrl", element, mimeType, scale, background));
    }

    // The markup and the data URL of a chart come back as the UTF-8 bytes of the text, read in chunks: a sizeable chart's
    // are far larger than the 32 KB one message of a Blazor Server circuit holds, and an interop result past it tears the
    // circuit down. An empty stream is a chart not rendered yet.
    private static async ValueTask<string?> ReadText(IJSStreamReference? streamReference)
    {
        if (streamReference is null) return null;

        await using (streamReference)
        {
            if (streamReference.Length == 0) return null;

            await using var stream = await streamReference.OpenReadStreamAsync(maxAllowedSize: streamReference.Length);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            return await reader.ReadToEndAsync();
        }
    }

    public static ValueTask BitChartDownloadText(this IJSRuntime jsRuntime, string fileName, string content, string mimeType)
    {
        return jsRuntime.InvokeScriptVoidAsync("BitBlazorUI.BitChart.downloadText", fileName, content, mimeType);
    }
}
