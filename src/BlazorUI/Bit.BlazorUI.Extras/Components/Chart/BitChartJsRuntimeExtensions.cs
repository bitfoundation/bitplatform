namespace Bit.BlazorUI;

internal static class BitChartJsRuntimeExtensions
{
    public static ValueTask<IJSObjectReference> BitChartObserve(this IJSRuntime jsRuntime,
                                                                     ElementReference element,
                                                                     DotNetObjectReference<BitChart> dotnetObj)
    {
        return jsRuntime.InvokeAsync<IJSObjectReference>("BitBlazorUI.BitChart.observe", element, dotnetObj);
    }

    public static ValueTask<IJSObjectReference> BitChartRegister(this IJSRuntime jsRuntime,
                                                                      ElementReference element,
                                                                      DotNetObjectReference<BitChart> dotnetObj,
                                                                      object options)
    {
        return jsRuntime.InvokeAsync<IJSObjectReference>("BitBlazorUI.BitChart.register", element, dotnetObj, options);
    }
}
