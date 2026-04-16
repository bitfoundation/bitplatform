namespace Bit.BlazorUI;

internal static class BitTagsInputJsRuntimeExtensions
{
    internal static ValueTask BitTagsInputSetup(this IJSRuntime jsRuntime, ElementReference input)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TagsInput.setup", input);
    }
}
