namespace Bit.BlazorUI.Legacy;

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
                                                        string? toolbarClass,
                                                        IEnumerable<QuillModule>? quillModules)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Legacy.RichTextEditor.setup",
            id, dotnetObj, editorContainer, toolbarContainer, theme, placeholder, readOnly, fullToolbar, toolbarStyle, toolbarClass, quillModules);
    }

    public static ValueTask<string> BitRichTextEditorGetText(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.Legacy.RichTextEditor.getText", id);
    }

    public static ValueTask<string> BitRichTextEditorGetHtml(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.Legacy.RichTextEditor.getHtml", id);
    }

    public static ValueTask<string> BitRichTextEditorGetContent(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.Legacy.RichTextEditor.getContent", id);
    }

    public static ValueTask BitRichTextEditorSetText(this IJSRuntime jsRuntime, string id, string? text)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Legacy.RichTextEditor.setText", id, text);
    }

    public static ValueTask BitRichTextEditorSetHtml(this IJSRuntime jsRuntime, string id, string? html)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Legacy.RichTextEditor.setHtml", id, html);
    }

    public static ValueTask BitRichTextEditorSetContent(this IJSRuntime jsRuntime, string id, string? content)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Legacy.RichTextEditor.setContent", id, content);
    }

    public static ValueTask BitRichTextEditorDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Legacy.RichTextEditor.dispose", id);
    }
}
