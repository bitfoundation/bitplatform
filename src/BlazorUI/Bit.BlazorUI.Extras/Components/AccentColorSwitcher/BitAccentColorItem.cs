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
    /// <see cref="BitThemeFactory"/> as the seed the whole palette is derived from. The <c>#</c> is
    /// optional and the casing is free: the value is canonicalized on every path that consumes it,
    /// so <c>8764B8</c> and <c>#8764b8</c> are the same accent. Anything that is not hex is not an
    /// accent - such an item is skipped rather than painted.
    /// </summary>
    public string Color { get; set; } = default!;
}
