namespace Bit.BlazorUI;

internal static class BitAiCopyButtonJsRuntimeExtensions
{
    internal static ValueTask<bool> BitAiCopyButtonCopy(this IJSRuntime jsRuntime, string text)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.AiCopyButton.copy", text);
    }
}
