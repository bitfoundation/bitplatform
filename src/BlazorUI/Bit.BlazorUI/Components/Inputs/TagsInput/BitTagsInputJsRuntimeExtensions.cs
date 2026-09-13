namespace Bit.BlazorUI;

internal static class BitTagsInputJsRuntimeExtensions
{
    internal static ValueTask BitTagsInputSetup(this IJSRuntime jsRuntime, ElementReference input, bool isEdit = false)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TagsInput.setup", input, isEdit);
    }

    internal static ValueTask BitTagsInputSetupTags(this IJSRuntime jsRuntime, ElementReference root)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TagsInput.setupTags", root);
    }
}
