namespace Bit.BlazorUI;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class BitMarkdownViewerJsRuntimeExtensions
{
    public static ValueTask<bool> BitMarkdownViewerCheckScript(this IJSRuntime jsRuntime, string script)
    {
        return jsRuntime.FastInvoke<bool>("BitBlazorUI.MarkdownViewer.checkScript", script);
    }

    public static async ValueTask<string> BitMarkdownViewerParse(this IJSRuntime jsRuntime, string markdown)
    {
        return OperatingSystem.IsBrowser() ? await jsRuntime.FastInvoke<string>("BitBlazorUI.MarkdownViewer.parse", markdown)
            : await jsRuntime.InvokeAsync<string>("BitBlazorUI.MarkdownViewer.parseAsync", markdown);
    }
}
