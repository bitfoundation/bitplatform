namespace Bit.BlazorUI;

/// <summary>
/// Event data when the document <c>bit-theme</c> attribute changes (resolved preset name, e.g. light/dark).
/// Values are normalized upstream so subscribers never observe null; an empty string indicates an unknown/unset theme.
/// </summary>
public sealed class BitThemeChangedEventArgs : EventArgs
{
    public BitThemeChangedEventArgs(string newTheme, string oldTheme)
    {
        ArgumentNullException.ThrowIfNull(newTheme);
        ArgumentNullException.ThrowIfNull(oldTheme);

        NewTheme = newTheme;
        OldTheme = oldTheme;
    }

    /// <summary>Resolved theme name now on <c>bit-theme</c>. Empty string if unknown/unset.</summary>
    public string NewTheme { get; }

    /// <summary>Previous resolved theme name. Empty string if unknown/unset.</summary>
    public string OldTheme { get; }
}
