namespace Bit.BlazorUI.Legacy;

internal static class LegacyJsRuntimeExtensions
{
    public static ValueTask BitLegacyInitScripts(this IJSRuntime jsRuntime, IEnumerable<string> scripts, bool isModule = false)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Legacy.Utils.initScripts", scripts, isModule);
    }

    public static ValueTask BitLegacyInitStylesheets(this IJSRuntime jsRuntime, IEnumerable<string> stylesheets)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Legacy.Utils.initStylesheets", stylesheets);
    }
}
