namespace Bit.BlazorUI;

internal static class BitMediaQueryJsRuntimeExtensions
{
    internal static ValueTask BitMediaQuerySetup(this IJSRuntime jsRuntime,
                                                      string key,
                                                      string? elementId,
                                                      string? query,
                                                      string? screenQuery,
                                                      Dictionary<string, string>? breakpoints,
                                                      DotNetObjectReference<BitMediaQuery>? dotnetObj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MediaQuery.setup", key, elementId, query, screenQuery, breakpoints, dotnetObj);
    }

    internal static ValueTask BitMediaQueryDispose(this IJSRuntime jsRuntime, string key)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MediaQuery.dispose", key);
    }
}
