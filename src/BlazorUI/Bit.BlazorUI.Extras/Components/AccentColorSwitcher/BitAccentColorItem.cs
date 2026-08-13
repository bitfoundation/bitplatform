namespace Bit.BlazorUI;

/// <summary>
/// An accent color offered by the <see cref="BitAccentColorSwitcher"/>.
/// </summary>
public class BitAccentColorItem
{
    /// <summary>
    /// The display name of the accent color, used as the swatch tooltip and accessible label.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The accessible label of the swatch button. When not set, an English label is composed from
    /// <see cref="Name"/> ("Apply the {Name} accent color") - set this to localize it.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// The accent color in <c>#RGB</c> or <c>#RRGGBB</c> hex format, fed to
    /// <see cref="BitThemeFactory"/> as the seed the whole palette is derived from.
    /// </summary>
    public string Color { get; set; } = default!;
}
