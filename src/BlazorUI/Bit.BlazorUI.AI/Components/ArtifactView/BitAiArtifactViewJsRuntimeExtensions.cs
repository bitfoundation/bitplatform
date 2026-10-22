namespace Bit.BlazorUI;

internal static class BitAiArtifactViewJsRuntimeExtensions
{
    internal static ValueTask BitAiArtifactViewDownloadText(this IJSRuntime jsRuntime, string fileName, string text, string mimeType)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiArtifactView.downloadText", fileName, text, mimeType);
    }
}
