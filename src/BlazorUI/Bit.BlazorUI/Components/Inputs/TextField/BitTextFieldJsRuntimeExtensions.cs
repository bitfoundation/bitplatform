namespace Bit.BlazorUI;

internal static class BitTextFieldJsRuntimeExtensions
{
    internal static ValueTask BitTextFieldSetupAutoHeight(this IJSRuntime jsRuntime, string id, ElementReference input)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.setupAutoHeight", id, input);
    }

    internal static ValueTask BitTextFieldDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.dispose", id);
    }
}
