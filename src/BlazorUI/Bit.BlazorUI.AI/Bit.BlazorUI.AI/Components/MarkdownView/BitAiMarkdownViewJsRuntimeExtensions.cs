namespace Bit.BlazorUI;

internal static class BitAiMarkdownViewJsRuntimeExtensions
{
    internal static ValueTask BitAiMarkdownViewFollow(this IJSRuntime jsRuntime, string targetId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiMarkdownView.follow", targetId);
    }
}
