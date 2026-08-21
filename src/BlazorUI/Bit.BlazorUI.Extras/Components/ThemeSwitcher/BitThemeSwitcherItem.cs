namespace Bit.BlazorUI;

/// <summary>
/// A design system offered by <see cref="BitThemeSwitcher"/>: a name to show, and the two theme names its
/// light and dark schemes are spelled with.
/// </summary>
public class BitThemeSwitcherItem
{
    /// <summary>
    /// The text shown for this design system in the picker. Falls back to <see cref="Value"/>.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The design system this item selects - the stem its two theme names share, e.g. <c>"material"</c> for
    /// the <c>material-light</c> / <c>material-dark</c> pair (see <see cref="BitExtraThemePresets"/>). It is
    /// also what identifies the item, so it has to be unique within one switcher, and it is matched against
    /// the applied theme name to decide which item is the selected one.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The theme name applied for this design system's light scheme. Defaults to <c>"{Value}-light"</c>.
    /// </summary>
    public string? LightTheme { get; set; }

    /// <summary>
    /// The theme name applied for this design system's dark scheme. Defaults to <c>"{Value}-dark"</c>.
    /// </summary>
    public string? DarkTheme { get; set; }

    /// <summary>
    /// The aria-label of this item in the picker. Falls back to <see cref="Text"/>.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Whether this design system can be selected.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
