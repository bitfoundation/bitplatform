namespace Bit.BlazorUI;

internal static class BitRichTextEditorJsRuntimeExtensions
{
    public static ValueTask BitRichTextEditorSetup(this IJSRuntime jsRuntime,
                                                        string id,
                                                        DotNetObjectReference<BitRichTextEditor>? dotnetObj,
                                                        ElementReference editorContainer,
                                                        ElementReference? toolbarContainer,
                                                        string? theme,
                                                        string? placeholder,
                                                        bool readOnly,
                                                        bool fullToolbar,
                                                        string? toolbarStyle,
                                                        string? toolbarClass)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.setup",
            id, dotnetObj, editorContainer, toolbarContainer, theme, placeholder, readOnly, fullToolbar, toolbarStyle, toolbarClass);
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

    public static ValueTask BitRichTextEditorSetText(this IJSRuntime jsRuntime, string id, string? text)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.setText", id, text);
    }

    public static ValueTask BitRichTextEditorSetHtml(this IJSRuntime jsRuntime, string id, string? html)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.setHtml", id, html);
    }

    public static ValueTask BitRichTextEditorSetContent(this IJSRuntime jsRuntime, string id, string? content)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.setContent", id, content);
    }
}
