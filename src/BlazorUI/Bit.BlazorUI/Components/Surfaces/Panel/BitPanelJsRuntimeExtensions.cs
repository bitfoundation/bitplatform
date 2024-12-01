namespace Bit.BlazorUI;

internal static class BitPanelJsRuntimeExtensions
{
    internal static ValueTask BitPanelSetup(this IJSRuntime js,
                                            string id,
                                            decimal trigger,
                                            int threshold,
                                            BitPanelPosition position,
                                            DotNetObjectReference<BitPanel>? dotnetObjectReference)
    {
        return js.InvokeVoid("BitBlazorUI.Panel.setup", id, trigger, threshold, position, dotnetObjectReference);
    }
}
