namespace Bit.BlazorUI;

/// <summary>
/// Event data when the document <c>bit-theme</c> attribute changes (resolved preset name, e.g. light/dark).
/// </summary>
public sealed class BitThemeChangedEventArgs : EventArgs
{
    public BitThemeChangedEventArgs(string newTheme, string oldTheme)
    {
        NewTheme = newTheme;
        OldTheme = oldTheme;
    }

    /// <summary>Resolved theme name now on <c>bit-theme</c>.</summary>
    public string NewTheme { get; }

    /// <summary>Previous resolved theme name.</summary>
    public string OldTheme { get; }
}
