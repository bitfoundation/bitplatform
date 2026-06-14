namespace Bit.BlazorUI;

internal static class BitMarkdownViewerJsRuntimeExtensions
{
    // FastInvoke returns null when the runtime can't service interop or a JSON/JS interop error is
    // swallowed on the in-process (WASM) path. Nullable distinguishes that from a legitimate false.
    public static ValueTask<bool?> BitMarkdownViewerCheckScriptLoaded(this IJSRuntime jsRuntime, string script)
    {
        return jsRuntime.FastInvoke<bool?>("BitBlazorUI.MarkdownViewer.checkScriptLoaded", script);
    }

    // FastInvoke/Invoke return null when the runtime can't service interop or a JSON/JS interop error
    // is swallowed on the in-process (WASM) path. Nullable surfaces that so call sites can coalesce.
    public static ValueTask<string?> BitMarkdownViewerParse(this IJSRuntime jsRuntime, string markdown, string? middleware)
    {
        return OperatingSystem.IsBrowser() && middleware.HasNoValue()
            ? jsRuntime.FastInvoke<string?>("BitBlazorUI.MarkdownViewer.parse", markdown)
            : jsRuntime.Invoke<string?>("BitBlazorUI.MarkdownViewer.parseAsync", markdown, middleware);
    }
}
