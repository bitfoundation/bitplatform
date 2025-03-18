namespace Bit.BlazorUI;

internal static class BitMarkdownEditorJsRuntimeExtensions
{
    public static ValueTask BitMarkdownEditorInit(this IJSRuntime jsRuntime, 
                                                       string id, 
                                                       ElementReference element, 
                                                       DotNetObjectReference<BitMarkdownEditor>? dotnetObj,
                                                       string? defaultValue)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.init", id, element, dotnetObj, defaultValue);
    }

    public static ValueTask<string> BitMarkdownEditorGetValue(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.MarkdownEditor.getValue", id);
    }

    public static ValueTask BitMarkdownEditorDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.dispose", id);
    }
}
