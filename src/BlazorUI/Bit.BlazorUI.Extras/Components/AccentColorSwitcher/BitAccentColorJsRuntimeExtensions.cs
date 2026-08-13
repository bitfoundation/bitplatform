namespace Bit.BlazorUI;

internal static class BitAccentColorJsRuntimeExtensions
{
    public static ValueTask<string?> BitAccentColorGetPersisted(this IJSRuntime jsRuntime, BitAccentColorPersistence persistence)
    {
        return jsRuntime.Invoke<string?>("BitBlazorUI.AccentColor.getPersisted", (int)persistence);
    }

    public static ValueTask BitAccentColorApply(this IJSRuntime jsRuntime, string token, string? css, string? version, bool setAttribute, BitAccentColorPersistence persistence)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AccentColor.apply", token, css, version, setAttribute, (int)persistence);
    }

    public static ValueTask BitAccentColorClear(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AccentColor.clear");
    }
}
