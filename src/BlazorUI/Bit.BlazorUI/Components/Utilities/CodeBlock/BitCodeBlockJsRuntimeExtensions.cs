namespace Bit.BlazorUI;

internal static class BitCodeBlockJsRuntimeExtensions
{
    internal static ValueTask<bool> BitCodeBlockCopy(this IJSRuntime js, ElementReference code, string? text, bool fromClick)
    {
        return js.Invoke<bool>("BitBlazorUI.CodeBlock.copy", code, text, fromClick);
    }
}
