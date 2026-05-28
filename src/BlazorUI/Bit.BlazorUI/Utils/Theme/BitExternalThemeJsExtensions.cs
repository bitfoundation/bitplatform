namespace Bit.BlazorUI;

internal static class BitExternalThemeJsExtensions
{
    internal static ValueTask BitExternalThemeAttach(this IJSRuntime js, string linkElementId, string href)
    {
        return js.InvokeVoid("BitBlazorUI.ExternalTheme.attach", linkElementId, href);
    }

    internal static ValueTask BitExternalThemeDetach(this IJSRuntime js, string linkElementId)
    {
        return js.InvokeVoid("BitBlazorUI.ExternalTheme.detach", linkElementId);
    }
}
