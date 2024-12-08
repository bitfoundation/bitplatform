﻿namespace Bit.BlazorUI;

internal static class BitPanelJsRuntimeExtensions
{
    internal static ValueTask BitPanelSetup(this IJSRuntime js,
                                            string id,
                                            decimal trigger,
                                            BitPanelPosition position,
                                            bool isRtl,
                                            DotNetObjectReference<BitPanel>? dotnetObjectReference)
    {
        return js.InvokeVoid("BitBlazorUI.Panel.setup", id, trigger, position, isRtl, dotnetObjectReference);
    }

    internal static ValueTask BitPanelDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Panel.dispose", id);
    }
}
