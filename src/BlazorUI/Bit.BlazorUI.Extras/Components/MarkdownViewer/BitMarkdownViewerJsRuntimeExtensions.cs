namespace Bit.BlazorUI;

internal static class BitMarkdownViewerJsRuntimeExtensions
{
    public static ValueTask<bool> BitMarkdownViewerCheckScriptLoaded(this IJSRuntime jsRuntime, string script)
    {
        return jsRuntime.FastInvoke<bool>("BitBlazorUI.MarkdownViewer.checkScriptLoaded", script);
    }

    public static ValueTask<string> BitMarkdownViewerParse(this IJSRuntime jsRuntime, string markdown, IEnumerable<string>? middlewares)
    {
        return OperatingSystem.IsBrowser()
            ? jsRuntime.FastInvoke<string>("BitBlazorUI.MarkdownViewer.parse", markdown, middlewares)
            : jsRuntime.Invoke<string>("BitBlazorUI.MarkdownViewer.parseAsync", markdown, middlewares);
    }
}
