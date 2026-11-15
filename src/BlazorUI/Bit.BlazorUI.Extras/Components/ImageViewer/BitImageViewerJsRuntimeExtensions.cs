using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitImageViewerJsRuntimeExtensions
{
    // The options are only ever constructed (never read) from C#, so without this hint the trimmer strips their getters
    // and the reflection-based interop serialization sends an empty object in trimmed builds.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitImageViewerJsOptions))]
    public static ValueTask<IJSObjectReference> BitImageViewerSetup(this IJSRuntime jsRuntime,
                                                                    ElementReference viewport,
                                                                    DotNetObjectReference<BitImageViewer> dotnetObj,
                                                                    BitImageViewerJsOptions options,
                                                                    double zoom)
    {
        return jsRuntime.InvokeScriptAsync<IJSObjectReference>("BitBlazorUI.BitImageViewer.setup", viewport, dotnetObj, options, zoom);
    }
}

// What the script of a BitImageViewer is told of its parameters, serialized to camelCase by the interop serializer to
// match what the script reads (min, max, factor, doubleClick, wheel, disabled).
internal sealed record BitImageViewerJsOptions
{
    public double Min { get; init; }
    public double Max { get; init; }
    public double Factor { get; init; }
    public double DoubleClick { get; init; }
    public bool Wheel { get; init; }
    public bool Disabled { get; init; }
}
