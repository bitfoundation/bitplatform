namespace Bit.BlazorUI;

/// <summary>
/// Event data for <see cref="BitThemeNotifications.ThemeChanged"/>: which preset replaced which.
/// </summary>
/// <remarks>
/// <para>
/// Both <see cref="NewTheme"/> and <see cref="OldTheme"/> are <see langword="null"/>-able strings.
/// <c>null</c> means "unknown" — typically because the document has no <c>bit-theme</c> attribute
/// yet (first paint before <see cref="BitThemeManager.SetThemeAsync"/>) or because the value
/// arrived from JS as <c>null</c>/<c>undefined</c>. Subscribers should branch on
/// <see cref="HasNewTheme"/>/<see cref="HasOldTheme"/> rather than comparing against the empty
/// string.
/// </para>
/// <para>
/// Earlier versions used <see cref="string.Empty"/> for this state, which conflicted with consumers
/// that legitimately wanted to detect the empty string. Switching to <see langword="null"/> is a
/// minor source-breaking change for any subscriber that pattern-matched <c>""</c>; update such
/// callers to <c>HasNewTheme</c>/<c>HasOldTheme</c> or <c>is null</c>.
/// </para>
/// </remarks>
public sealed class BitThemeChangedEventArgs : EventArgs
{
    public BitThemeChangedEventArgs(string? newTheme, string? oldTheme)
    {
        NewTheme = newTheme;
        OldTheme = oldTheme;
    }

    /// <summary>Resolved theme name now on <c>bit-theme</c>, or <see langword="null"/> if unknown/unset.</summary>
    public string? NewTheme { get; }

    /// <summary>Previous resolved theme name, or <see langword="null"/> if unknown/unset.</summary>
    public string? OldTheme { get; }

    /// <summary>True when <see cref="NewTheme"/> is non-null and non-whitespace.</summary>
    public bool HasNewTheme => !string.IsNullOrWhiteSpace(NewTheme);

    /// <summary>True when <see cref="OldTheme"/> is non-null and non-whitespace.</summary>
    public bool HasOldTheme => !string.IsNullOrWhiteSpace(OldTheme);
}
