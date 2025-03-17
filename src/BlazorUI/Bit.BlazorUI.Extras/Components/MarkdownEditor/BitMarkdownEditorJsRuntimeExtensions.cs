namespace Bit.BlazorUI;

internal static class BitMarkdownEditorJsRuntimeExtensions
{
    public static ValueTask BitMarkdownEditorInit(this IJSRuntime jsRuntime, string id, ElementReference element)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.init", id, element);
    }

    public static ValueTask<string> BitMarkdownEditorGetValue(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.MarkdownEditor.getValue", id);
    }
}
