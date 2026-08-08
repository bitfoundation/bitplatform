namespace Bit.BlazorUI;

internal static class BitTextFieldJsRuntimeExtensions
{
    internal static ValueTask BitTextFieldSetupMultilineInput(this IJSRuntime jsRuntime, string id, ElementReference input, bool autoHeight, bool preventEnter, int? maxRows)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.setupMultilineInput", id, input, autoHeight, preventEnter, maxRows);
    }

    internal static ValueTask BitTextFieldAdjustHeight(this IJSRuntime jsRuntime, string id, ElementReference input, int? maxRows)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.adjustHeight", id, input, maxRows);
    }

    internal static ValueTask BitTextFieldSetupGhostText(this IJSRuntime jsRuntime, string id, ElementReference input, DotNetObjectReference<BitTextField> dotnetObj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.setupGhostText", id, input, dotnetObj);
    }

    internal static ValueTask BitTextFieldSetGhostText(this IJSRuntime jsRuntime, string id, string ghostText)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.setGhostText", id, ghostText);
    }

    internal static ValueTask BitTextFieldDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.dispose", id);
    }
}
