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

    internal static ValueTask BitTextFieldSetupComposition(this IJSRuntime jsRuntime, string id, ElementReference input)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.setupComposition", id, input);
    }

    internal static ValueTask BitTextFieldSetupSelectOnFocus(this IJSRuntime jsRuntime, string id, ElementReference input)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.setupSelectOnFocus", id, input);
    }

    internal static ValueTask BitTextFieldSetupGhostText(this IJSRuntime jsRuntime, string id, ElementReference input, DotNetObjectReference<BitTextField> dotnetObj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.setupGhostText", id, input, dotnetObj);
    }

    internal static ValueTask BitTextFieldSetGhostText(this IJSRuntime jsRuntime, string id, string ghostText)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.setGhostText", id, ghostText);
    }

    internal static ValueTask BitTextFieldSetSelectionRange(this IJSRuntime jsRuntime, ElementReference input, int? start, int? end)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.setSelectionRange", input, start, end);
    }

    internal static ValueTask BitTextFieldDisposeFeature(this IJSRuntime jsRuntime, string id, string feature)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.disposeFeature", id, feature);
    }

    internal static ValueTask BitTextFieldDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TextField.dispose", id);
    }
}
