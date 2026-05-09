namespace Bit.BlazorUI;

/// <summary>
/// Bridges Blazor to the client <c>BitTheme</c> script: preset names on the <c>bit-theme</c> attribute vs inline CSS variables from <see cref="BitTheme"/>.
/// <see cref="SetThemeAsync"/> selects packaged Fluent CSS for <c>:root[bit-theme]</c>.
/// <see cref="ApplyBitThemeAsync"/> sets <c>--bit-*</c> variables on the target element (default <c>document.body</c>), overriding stylesheet defaults for that subtree.
/// Nested <see cref="BitThemeProvider"/> scopes overrides to its root element.
/// </summary>
public class BitThemeManager
{
    private IJSRuntime _js = default!;

    public BitThemeManager(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>Returns the active <c>bit-theme</c> name from the document element.</summary>
    public async ValueTask<string> GetCurrentThemeAsync()
    {
        return await _js.BitThemeGetCurrentTheme();
    }

    /// <summary>Sets the <c>bit-theme</c> attribute (use values from <see cref="BitThemePresets"/> or custom names matching your CSS).</summary>
    public async ValueTask<string> SetThemeAsync(string themeName)
    {
        return await _js.BitThemeSetTheme(themeName);
    }

    /// <summary>Toggles between configured light and dark theme names.</summary>
    public async ValueTask<string> ToggleDarkLightAsync()
    {
        return await _js.BitThemeToggleThemeDarkLight();
    }

    /// <summary>Applies <paramref name="bitTheme"/> as CSS custom properties on <paramref name="element"/> (default: body), overriding stylesheet tokens for that subtree.</summary>
    public async ValueTask ApplyBitThemeAsync(BitTheme? bitTheme, ElementReference? element = null)
    {
        await _js.BitThemeApplyBitTheme(BitThemeUtilities.ToCssVariables(bitTheme), element);
    }

    public async ValueTask<bool> IsSystemInDarkMode()
    {
        return await _js.BitThemeIsSystemDark();
    }

    public async ValueTask<string> GetCurrentPersistedThemeAsync()
    {
        return await _js.BitThemeGetCurrentPersistedTheme();
    }
}
