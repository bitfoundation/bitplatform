namespace Bit.BlazorUI.Legacy;

internal static class BitMarkdownEditorJsRuntimeExtensions
{
    public static ValueTask BitMarkdownEditorInit(this IJSRuntime jsRuntime, 
                                                       string id, 
                                                       ElementReference element, 
                                                       DotNetObjectReference<BitMarkdownEditor>? dotnetObj,
                                                       string? defaultValue)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Legacy.MarkdownEditor.init", id, element, dotnetObj, defaultValue);
    }

    public static ValueTask<string> BitMarkdownEditorGetValue(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.Legacy.MarkdownEditor.getValue", id);
    }

    public static ValueTask<string> BitMarkdownEditorSetValue(this IJSRuntime jsRuntime, string id, string? value)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.Legacy.MarkdownEditor.setValue", id, value);
    }

    public static ValueTask<string> BitMarkdownEditorRun(this IJSRuntime jsRuntime, string id, string command)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.Legacy.MarkdownEditor.run", id, command);
    }

    public static ValueTask<string> BitMarkdownEditorAdd(this IJSRuntime jsRuntime, string id, string value, string type)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.Legacy.MarkdownEditor.add", id, value, type);
    }

    public static ValueTask BitMarkdownEditorDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Legacy.MarkdownEditor.dispose", id);
    }
}
