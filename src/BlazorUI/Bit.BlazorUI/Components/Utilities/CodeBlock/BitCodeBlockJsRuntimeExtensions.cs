namespace Bit.BlazorUI;

internal static class BitCodeBlockJsRuntimeExtensions
{
    // True once the code is on the clipboard, false when the clipboard refused it, and null when no copy was attempted (a
    // click on the code that made a selection, a block whose content renders no text).
    internal static ValueTask<bool?> BitCodeBlockCopy(this IJSRuntime js, ElementReference code, string? text, bool fromClick)
    {
        return js.Invoke<bool?>("BitBlazorUI.CodeBlock.copy", code, text, fromClick);
    }
}
