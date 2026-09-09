namespace Bit.BlazorUI;

internal static class BitMarkdownEditorJsRuntimeExtensions
{
    public static ValueTask BitMarkdownEditorInit(this IJSRuntime jsRuntime,
                                                       string id,
                                                       ElementReference textArea,
                                                       ElementReference root,
                                                       DotNetObjectReference<BitMarkdownEditor>? dotnetObj,
                                                       string? defaultValue,
                                                       BitMarkdownEditorConfig config)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.init", id, textArea, root, dotnetObj, defaultValue, config);
    }

    public static ValueTask BitMarkdownEditorSetConfig(this IJSRuntime jsRuntime, string id, BitMarkdownEditorConfig config)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.setConfig", id, config);
    }

    public static ValueTask<string> BitMarkdownEditorGetValue(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.MarkdownEditor.getValue", id);
    }

    public static ValueTask BitMarkdownEditorSetValue(this IJSRuntime jsRuntime, string id, string? value)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.setValue", id, value);
    }

    public static ValueTask BitMarkdownEditorRun(this IJSRuntime jsRuntime, string id, string command)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.run", id, command);
    }

    public static ValueTask BitMarkdownEditorInsert(this IJSRuntime jsRuntime, string id, string text)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.insert", id, text);
    }

    public static ValueTask<int> BitMarkdownEditorReplaceAll(this IJSRuntime jsRuntime, string id, string search, string replacement, bool all, bool matchCase)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.MarkdownEditor.replaceAll", id, search, replacement, all, matchCase);
    }

    public static ValueTask<BitMarkdownEditorFindResult> BitMarkdownEditorFind(this IJSRuntime jsRuntime, string id, string search, bool matchCase, bool backwards)
    {
        return jsRuntime.Invoke<BitMarkdownEditorFindResult>("BitBlazorUI.MarkdownEditor.find", id, search, matchCase, backwards);
    }

    public static ValueTask<BitMarkdownEditorFindResult> BitMarkdownEditorReplaceOne(this IJSRuntime jsRuntime, string id, string search, string replacement, bool matchCase)
    {
        return jsRuntime.Invoke<BitMarkdownEditorFindResult>("BitBlazorUI.MarkdownEditor.replaceOne", id, search, replacement, matchCase);
    }

    public static ValueTask<BitMarkdownEditorSelection> BitMarkdownEditorGetSelection(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<BitMarkdownEditorSelection>("BitBlazorUI.MarkdownEditor.getSelection", id);
    }

    public static ValueTask BitMarkdownEditorSetSelection(this IJSRuntime jsRuntime, string id, int start, int end)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.setSelection", id, start, end);
    }

    public static ValueTask BitMarkdownEditorClearDraft(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.clearDraft", id);
    }

    public static ValueTask BitMarkdownEditorUndo(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.undo", id);
    }

    public static ValueTask BitMarkdownEditorRedo(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.redo", id);
    }

    public static ValueTask BitMarkdownEditorFocus(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.focus", id);
    }

    public static ValueTask BitMarkdownEditorBlur(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.blur", id);
    }

    public static ValueTask BitMarkdownEditorDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.dispose", id);
    }
}
