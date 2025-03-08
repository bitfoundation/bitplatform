namespace Bit.BlazorUI;

internal static class BitMarkdownViewerJsRuntimeExtensions
{
    public static ValueTask BitMarkdownViewerInit(this IJSRuntime jsRuntime, IEnumerable<string> scripts)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MarkdownViewer.init", scripts);
    }

    public static ValueTask<string> BitMarkdownViewerParse(this IJSRuntime jsRuntime, string markdown)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.MarkdownViewer.parse", markdown);
    }
}
