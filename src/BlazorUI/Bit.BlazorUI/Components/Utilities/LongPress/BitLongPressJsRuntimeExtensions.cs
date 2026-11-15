namespace Bit.BlazorUI;

internal static class BitLongPressJsRuntimeExtensions
{
    internal static ValueTask BitLongPressSetup(this IJSRuntime js,
                                                     string id,
                                                     ElementReference element,
                                                     int delay,
                                                     decimal moveTolerance,
                                                     bool touchOnly,
                                                     bool keyboardTrigger,
                                                     DotNetObjectReference<BitLongPress>? dotnetObjectReference)
    {
        return js.InvokeVoid("BitBlazorUI.LongPress.setup", id, element, delay, moveTolerance, touchOnly, keyboardTrigger, dotnetObjectReference);
    }

    internal static ValueTask BitLongPressDispose(this IJSRuntime js, string id)
    {
        return js.InvokeVoid("BitBlazorUI.LongPress.dispose", id);
    }
}
