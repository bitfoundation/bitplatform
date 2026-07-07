namespace Bit.BlazorUI;

internal static class BitMarkdownEditorJsRuntimeExtensions
{
    public static ValueTask BitMarkdownEditorInit(this IJSRuntime jsRuntime,
                                                       string id,
                                                       ElementReference textArea,
                                                       ElementReference root,
                                                       DotNetObjectReference<BitMarkdownEditor>? dotnetObj,
                                                       string? defaultValue)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.init", id, textArea, root, dotnetObj, defaultValue);
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

    public static ValueTask BitMarkdownEditorDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownEditor.dispose", id);
    }
}
