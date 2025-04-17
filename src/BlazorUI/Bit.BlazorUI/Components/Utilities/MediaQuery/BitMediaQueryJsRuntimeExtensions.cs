namespace Bit.BlazorUI;

internal static class BitMediaQueryJsRuntimeExtensions
{
    internal static ValueTask BitMediaQuerySetup(this IJSRuntime jsRuntime, 
                                                      string id, 
                                                      string query, 
                                                      DotNetObjectReference<BitMediaQuery>? dotnetObj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MediaQuery.setup", id, query, dotnetObj);
    }

    internal static ValueTask BitMediaQueryDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MediaQuery.dispose", id);
    }
}
