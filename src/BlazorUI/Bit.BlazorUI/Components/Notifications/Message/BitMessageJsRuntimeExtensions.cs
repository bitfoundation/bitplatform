namespace Bit.BlazorUI;

internal static class BitMessageJsRuntimeExtensions
{
    internal static ValueTask BitMessageObserveOverflow(this IJSRuntime jsRuntime,
                                                        string id,
                                                        ElementReference root,
                                                        DotNetObjectReference<BitMessage> dotnetObj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Message.observeOverflow", id, root, dotnetObj);
    }

    internal static ValueTask BitMessageDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Message.dispose", id);
    }
}
