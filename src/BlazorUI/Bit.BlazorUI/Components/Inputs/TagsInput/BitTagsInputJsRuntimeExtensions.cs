namespace Bit.BlazorUI;

internal static class BitTagsInputJsRuntimeExtensions
{
    internal static ValueTask BitTagsInputSetup(this IJSRuntime jsRuntime, ElementReference input, bool isEdit = false, DotNetObjectReference<BitTagsInput>? dotnetObj = null)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TagsInput.setup", input, isEdit, dotnetObj);
    }

    internal static ValueTask BitTagsInputSetupTags(this IJSRuntime jsRuntime, ElementReference root)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.TagsInput.setupTags", root);
    }
}
