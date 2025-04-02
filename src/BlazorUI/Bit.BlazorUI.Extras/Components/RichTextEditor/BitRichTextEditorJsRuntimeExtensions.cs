namespace Bit.BlazorUI;

internal static class BitRichTextEditorJsRuntimeExtensions
{
    public static ValueTask BitRichTextEditorSetup(this IJSRuntime jsRuntime, 
                                                        string id, 
                                                        DotNetObjectReference<BitRichTextEditor>? dotnetObj,
                                                        ElementReference editorContainer, 
                                                        ElementReference toolbarContainer,
                                                        string? theme)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.setup", id, dotnetObj, editorContainer, toolbarContainer, theme);
    }

    public static ValueTask<string> BitRichTextEditorGetText(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.RichTextEditor.getText", id);
    }

    public static ValueTask<string> BitRichTextEditorGetHtml(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.RichTextEditor.getHtml", id);
    }

    public static ValueTask<string> BitRichTextEditorGetContent(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.RichTextEditor.getContent", id);
    }
}
