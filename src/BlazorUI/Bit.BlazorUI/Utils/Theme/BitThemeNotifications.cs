namespace Bit.BlazorUI;

/// <summary>
/// Raised when the global <c>bit-theme</c> document attribute changes (including OS-driven updates when following system theme).
/// Subscribe in scoped components; requires <see cref="BitThemeManager"/> interop at least once per circuit so the client script can notify .NET.
/// </summary>
public sealed class BitThemeNotifications
{
    /// <summary>Fires after <c>BitTheme.set</c>, <c>toggleDarkLight</c>, or <c>prefers-color-scheme</c> updates while following system theme.</summary>
    public event EventHandler<BitThemeChangedEventArgs>? ThemeChanged;

    internal void Raise(string newTheme, string oldTheme)
    {
        ThemeChanged?.Invoke(this, new BitThemeChangedEventArgs(newTheme, oldTheme));
    }
}
