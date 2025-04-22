namespace Bit.BlazorUI;

internal static class BitTextFieldJsRuntimeExtensions
{
    internal static ValueTask BitTextFieldSetupMultilineInput(this IJSRuntime jsRuntime, string id, ElementReference input, bool autoHeight, bool preventEnter)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.setupMultilineInput", id, input, autoHeight, preventEnter);
    }

    internal static ValueTask BitTextFieldAdjustHeight(this IJSRuntime jsRuntime, ElementReference input)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.adjustHeight", input);
    }

    internal static ValueTask BitTextFieldDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.dispose", id);
    }
}
