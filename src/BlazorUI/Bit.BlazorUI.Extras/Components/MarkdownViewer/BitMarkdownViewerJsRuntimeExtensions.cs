namespace Bit.BlazorUI;

internal static class BitMarkdownViewerJsRuntimeExtensions
{
    public static ValueTask BitMarkdownViewerInit(this IJSRuntime jsRuntime, IEnumerable<string> scripts)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownViewer.init", scripts);
    }
}
