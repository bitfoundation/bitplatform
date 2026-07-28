namespace Bit.BlazorUI;

internal static class BitMediaQueryJsRuntimeExtensions
{
    internal static ValueTask BitMediaQuerySetup(this IJSRuntime jsRuntime,
                                                      string id,
                                                      string? query,
                                                      string? screenQuery,
                                                      DotNetObjectReference<BitMediaQuery>? dotnetObj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MediaQuery.setup", id, query, screenQuery, dotnetObj);
    }

    internal static ValueTask BitMediaQueryDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MediaQuery.dispose", id);
    }
}
