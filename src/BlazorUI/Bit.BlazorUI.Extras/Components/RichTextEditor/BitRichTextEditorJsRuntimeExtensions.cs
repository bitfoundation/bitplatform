namespace Bit.BlazorUI;

internal static class BitRichTextEditorJsRuntimeExtensions
{
    public static ValueTask BitRichTextEditorSetup(this IJSRuntime jsRuntime,
                                                        ElementReference editor,
                                                        DotNetObjectReference<BitRichTextEditor>? dotnetObj,
                                                        BitRichTextEditorSetupOptions options)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.initialize", editor, dotnetObj, options);
    }

    public static ValueTask BitRichTextEditorUpdateOptions(this IJSRuntime jsRuntime,
                                                           ElementReference editor,
                                                           BitRichTextEditorSetupOptions options)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.updateOptions", editor, options);
    }

    public static ValueTask BitRichTextEditorEnableToolbarRoving(this IJSRuntime jsRuntime, ElementReference toolbar)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.enableToolbarRoving", toolbar);
    }

    public static ValueTask BitRichTextEditorDispose(this IJSRuntime jsRuntime, ElementReference editor)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.dispose", editor);
    }

    public static ValueTask BitRichTextEditorFocus(this IJSRuntime jsRuntime, ElementReference editor)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.focus", editor);
    }

    public static ValueTask<string> BitRichTextEditorGetHtml(this IJSRuntime jsRuntime, ElementReference editor)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.RichTextEditor.getHtml", editor);
    }

    public static ValueTask BitRichTextEditorSetHtml(this IJSRuntime jsRuntime, ElementReference editor, string? html)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.setHtml", editor, html);
    }

    public static ValueTask<string> BitRichTextEditorSanitizeHtml(this IJSRuntime jsRuntime, ElementReference editor, string? html)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.RichTextEditor.sanitizeHtml", editor, html);
    }

    public static ValueTask<bool> BitRichTextEditorValidateHtml(this IJSRuntime jsRuntime, ElementReference editor, string? html)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.RichTextEditor.validateHtml", editor, html);
    }

    public static ValueTask BitRichTextEditorExec(this IJSRuntime jsRuntime, ElementReference editor, string command, string? value)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.exec", editor, command, value);
    }

    public static ValueTask BitRichTextEditorExecBlock(this IJSRuntime jsRuntime, ElementReference editor, string tag)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.execBlock", editor, tag);
    }

    public static ValueTask BitRichTextEditorCreateLink(this IJSRuntime jsRuntime, ElementReference editor, string url)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.createLink", editor, url);
    }

    public static ValueTask BitRichTextEditorUpdateLink(this IJSRuntime jsRuntime, ElementReference editor, string url)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.updateLink", editor, url);
    }

    public static ValueTask BitRichTextEditorInsertImageUrl(this IJSRuntime jsRuntime, ElementReference editor, string url)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.insertImageUrl", editor, url);
    }

    public static ValueTask BitRichTextEditorApplyColor(this IJSRuntime jsRuntime, ElementReference editor, string kind, string value)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.applyColor", editor, kind, value);
    }

    public static ValueTask BitRichTextEditorApplyFont(this IJSRuntime jsRuntime, ElementReference editor, string kind, string value)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.applyFont", editor, kind, value);
    }

    public static ValueTask<bool> BitRichTextEditorInsertMedia(this IJSRuntime jsRuntime, ElementReference editor, string html)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.RichTextEditor.insertMedia", editor, html);
    }

    public static ValueTask BitRichTextEditorInsertText(this IJSRuntime jsRuntime, ElementReference editor, string text)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.insertText", editor, text);
    }

    public static ValueTask BitRichTextEditorInsertTable(this IJSRuntime jsRuntime, ElementReference editor, int rows, int cols)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.insertTable", editor, rows, cols);
    }

    public static ValueTask BitRichTextEditorTableOp(this IJSRuntime jsRuntime, ElementReference editor, string op)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.tableOp", editor, op);
    }

    public static ValueTask BitRichTextEditorClearFind(this IJSRuntime jsRuntime, ElementReference editor)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.clearFind", editor);
    }

    public static ValueTask<int> BitRichTextEditorFind(this IJSRuntime jsRuntime, ElementReference editor, string term, bool caseSensitive)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.RichTextEditor.find", editor, term, caseSensitive);
    }

    public static ValueTask BitRichTextEditorReplaceCurrent(this IJSRuntime jsRuntime, ElementReference editor, string term, string replacement, bool caseSensitive)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.replaceCurrent", editor, term, replacement, caseSensitive);
    }

    public static ValueTask<int> BitRichTextEditorReplaceAll(this IJSRuntime jsRuntime, ElementReference editor, string term, string replacement, bool caseSensitive)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.RichTextEditor.replaceAll", editor, term, replacement, caseSensitive);
    }

    public static ValueTask BitRichTextEditorSetFullScreen(this IJSRuntime jsRuntime, ElementReference editor, bool on)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.setFullScreen", editor, on);
    }

    public static ValueTask BitRichTextEditorSetBlockDirection(this IJSRuntime jsRuntime, ElementReference editor, string dir)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.setBlockDirection", editor, dir);
    }

    public static ValueTask BitRichTextEditorBindSlashKeys(this IJSRuntime jsRuntime, ElementReference input)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.bindSlashKeys", input);
    }

    public static ValueTask BitRichTextEditorApplySlashCommand(this IJSRuntime jsRuntime, ElementReference editor, string command)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.applySlashCommand", editor, command);
    }
}
