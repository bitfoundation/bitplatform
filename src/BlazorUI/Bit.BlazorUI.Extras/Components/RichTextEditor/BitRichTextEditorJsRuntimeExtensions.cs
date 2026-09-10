using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitRichTextEditorJsRuntimeExtensions
{
    // The setup payload types are only ever constructed (never read) from C#, so without these
    // hints the trimmer strips their property getters and the reflection-based interop
    // serialization silently sends empty objects to the bridge in trimmed (release) builds.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitRichTextEditorSetupOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitRichTextEditorPolicyPayload))]
    public static ValueTask BitRichTextEditorSetup(this IJSRuntime jsRuntime,
                                                        ElementReference editor,
                                                        DotNetObjectReference<BitRichTextEditor>? dotnetObj,
                                                        BitRichTextEditorSetupOptions options)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.initialize", editor, dotnetObj, options);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitRichTextEditorSetupOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitRichTextEditorPolicyPayload))]
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

    public static ValueTask<string> BitRichTextEditorGetText(this IJSRuntime jsRuntime, ElementReference editor)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.RichTextEditor.getText", editor);
    }

    public static ValueTask<string> BitRichTextEditorHtmlToText(this IJSRuntime jsRuntime, ElementReference editor, string? html)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.RichTextEditor.htmlToText", editor, html);
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

    public static ValueTask BitRichTextEditorCreateLink(this IJSRuntime jsRuntime, ElementReference editor, string url, bool newTab, string? text)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.createLink", editor, url, newTab, text);
    }

    public static ValueTask BitRichTextEditorUpdateLink(this IJSRuntime jsRuntime, ElementReference editor, string url, bool newTab)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.updateLink", editor, url, newTab);
    }

    public static ValueTask BitRichTextEditorInsertImageUrl(this IJSRuntime jsRuntime, ElementReference editor, string url, string? alt)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.insertImageUrl", editor, url, alt);
    }

    public static ValueTask BitRichTextEditorAlignImage(this IJSRuntime jsRuntime, ElementReference editor, string align)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.alignImage", editor, align);
    }

    public static ValueTask BitRichTextEditorInsertHtml(this IJSRuntime jsRuntime, ElementReference editor, string html)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.insertHtml", editor, html);
    }

    public static ValueTask BitRichTextEditorSelectAll(this IJSRuntime jsRuntime, ElementReference editor)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.selectAll", editor);
    }

    public static ValueTask<string> BitRichTextEditorGetSelectedText(this IJSRuntime jsRuntime, ElementReference editor)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.RichTextEditor.getSelectedText", editor);
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

    public static ValueTask BitRichTextEditorInsertTable(this IJSRuntime jsRuntime, ElementReference editor, int rows, int cols, bool header)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.insertTable", editor, rows, cols, header);
    }

    public static ValueTask BitRichTextEditorTableOp(this IJSRuntime jsRuntime, ElementReference editor, string op)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.tableOp", editor, op);
    }

    public static ValueTask BitRichTextEditorClearFind(this IJSRuntime jsRuntime, ElementReference editor)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.clearFind", editor);
    }

    public static ValueTask<int> BitRichTextEditorFind(this IJSRuntime jsRuntime, ElementReference editor, string term, bool caseSensitive, bool wholeWord)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.RichTextEditor.find", editor, term, caseSensitive, wholeWord);
    }

    public static ValueTask<int> BitRichTextEditorFindStep(this IJSRuntime jsRuntime, ElementReference editor, int delta)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.RichTextEditor.findStep", editor, delta);
    }

    public static ValueTask<int> BitRichTextEditorReplaceCurrent(this IJSRuntime jsRuntime, ElementReference editor, string term, string replacement, bool caseSensitive, bool wholeWord)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.RichTextEditor.replaceCurrent", editor, term, replacement, caseSensitive, wholeWord);
    }

    public static ValueTask<int> BitRichTextEditorReplaceAll(this IJSRuntime jsRuntime, ElementReference editor, string term, string replacement, bool caseSensitive, bool wholeWord)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.RichTextEditor.replaceAll", editor, term, replacement, caseSensitive, wholeWord);
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

    public static ValueTask BitRichTextEditorApplyMention(this IJSRuntime jsRuntime, ElementReference editor, string html)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.applyMention", editor, html);
    }

    public static ValueTask BitRichTextEditorApplySlashCommand(this IJSRuntime jsRuntime, ElementReference editor, string command)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.applySlashCommand", editor, command);
    }
}
