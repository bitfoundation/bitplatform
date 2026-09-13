using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The CSS classes a component derives from the parameters every component shares. The mapping from a value
/// to a suffix is the same everywhere; only the prefix of the component differs, so it is passed in rather
/// than the whole table being written out again in each of them.
/// </summary>
internal static class BitCssClasses
{
    /// <summary>
    /// The color class of a component, e.g. <c>bit-tpc-pri</c> for the primary color of a TimePicker.
    /// </summary>
    /// <param name="color">The color, which falls back to the primary one when it is not set.</param>
    /// <param name="prefix">The class prefix of the component, e.g. <c>bit-tpc</c>.</param>
    public static string Color(BitColor? color, string prefix)
    {
        var suffix = color switch
        {
            BitColor.Primary => "pri",
            BitColor.Secondary => "sec",
            BitColor.Tertiary => "ter",
            BitColor.Info => "inf",
            BitColor.Success => "suc",
            BitColor.Warning => "wrn",
            BitColor.SevereWarning => "swr",
            BitColor.Error => "err",
            BitColor.PrimaryBackground => "pbg",
            BitColor.SecondaryBackground => "sbg",
            BitColor.TertiaryBackground => "tbg",
            BitColor.PrimaryForeground => "pfg",
            BitColor.SecondaryForeground => "sfg",
            BitColor.TertiaryForeground => "tfg",
            BitColor.PrimaryBorder => "pbr",
            BitColor.SecondaryBorder => "sbr",
            BitColor.TertiaryBorder => "tbr",
            _ => "pri"
        };

        return $"{prefix}-{suffix}";
    }

    /// <summary>
    /// The size class of a component, e.g. <c>bit-tpc-sm</c> for a small TimePicker. A size that is not set
    /// has no class of its own: the component is left at whatever its stylesheet makes the default.
    /// </summary>
    /// <param name="size">The size.</param>
    /// <param name="prefix">The class prefix of the component, e.g. <c>bit-tpc</c>.</param>
    public static string Size(BitSize? size, string prefix)
    {
        var suffix = size switch
        {
            BitSize.Small => "sm",
            BitSize.Medium => "md",
            BitSize.Large => "lg",
            _ => null
        };

        return suffix is null ? string.Empty : $"{prefix}-{suffix}";
    }

    /// <summary>
    /// Whether a component lays itself out right to left: the direction it was given, and without one the
    /// direction its culture reads in - a culture that writes right to left implies the direction as well.
    /// </summary>
    public static bool IsRtl(BitDir? dir, CultureInfo culture)
    {
        if (dir is not null) return dir == BitDir.Rtl;

        return culture.TextInfo.IsRightToLeft;
    }

    /// <summary>
    /// The <c>bit-rtl</c> class a component needs only when its culture is what makes it right to left. A
    /// direction it was given explicitly is rendered as the <c>dir</c> attribute of the element instead, so
    /// adding the class for that case as well would say the same thing twice.
    /// </summary>
    public static string CultureRtl(BitDir? dir, CultureInfo culture)
    {
        return (dir is null && culture.TextInfo.IsRightToLeft) ? "bit-rtl" : string.Empty;
    }
}
