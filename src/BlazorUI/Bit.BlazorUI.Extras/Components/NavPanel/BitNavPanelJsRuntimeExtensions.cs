namespace Bit.BlazorUI;

internal static class BitNavPanelJsRuntimeExtensions
{
    // Answers synchronously on WebAssembly, where the call is made in process; see BitNavPanel.IsDrawerAtStart.
    internal static ValueTask<bool> BitNavPanelIsDrawer(this IJSRuntime jsRuntime, string breakpoint, string? themedBreakpoint)
    {
        return jsRuntime.FastInvoke<bool>("BitBlazorUI.NavPanel.isDrawer", breakpoint, themedBreakpoint);
    }
}
