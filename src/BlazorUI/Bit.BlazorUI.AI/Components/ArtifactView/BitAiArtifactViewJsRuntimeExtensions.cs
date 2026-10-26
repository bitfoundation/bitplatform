namespace Bit.BlazorUI;

internal static class BitAiArtifactViewJsRuntimeExtensions
{
    internal static ValueTask BitAiArtifactViewDownloadText(this IJSRuntime jsRuntime, string fileName, string text, string mimeType)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiArtifactView.downloadText", fileName, text, mimeType);
    }

    internal static ValueTask BitAiArtifactViewExpand(this IJSRuntime jsRuntime, string panelId, DotNetObjectReference<BitAiArtifactView> dotnetObj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiArtifactView.expand", panelId, dotnetObj);
    }

    internal static ValueTask BitAiArtifactViewCollapse(this IJSRuntime jsRuntime, string panelId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiArtifactView.collapse", panelId);
    }
}
