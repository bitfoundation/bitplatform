namespace Bit.BlazorUI;

public class BitThemeSwitcherClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitThemeSwitcher.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the design system picker of the BitThemeSwitcher.
    /// </summary>
    public string? DesignSystem { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for both color scheme buttons of the BitThemeSwitcher.
    /// </summary>
    public string? ColorSchemeButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the color scheme button that is shown while the dark scheme is active
    /// (the one that switches to light) of the BitThemeSwitcher.
    /// </summary>
    public string? DarkSchemeButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the color scheme button that is shown while the light scheme is active
    /// (the one that switches to dark) of the BitThemeSwitcher.
    /// </summary>
    public string? LightSchemeButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of each color scheme button of the BitThemeSwitcher.
    /// </summary>
    public string? Icon { get; set; }
}
