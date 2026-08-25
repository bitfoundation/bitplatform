namespace Bit.BlazorUI;

public class BitThemeTypography
{
    public string? FontFamily { get; set; }
    public string? FontWeight { get; set; }
    public string? LineHeight { get; set; }
    public string? GutterSize { get; set; }

    /// <summary>The font-size scale components read their text and glyph sizes from (<c>--bit-tpg-fs-*</c>).</summary>
    public BitThemeTypographyFontSizes FontSize { get; set; } = new();

    /// <summary>The font-weight scale components read their weights from (<c>--bit-tpg-fw-*</c>).</summary>
    public BitThemeTypographyFontWeights FontWeights { get; set; } = new();

    /// <summary>The tracking and case of the label of an interactive control (<c>--bit-tpg-ctrl-*</c>).</summary>
    public BitThemeControlTypography Control { get; set; } = new();

    public BitThemeTypographyVariants H1 { get; set; } = new();
    public BitThemeTypographyVariants H2 { get; set; } = new();
    public BitThemeTypographyVariants H3 { get; set; } = new();
    public BitThemeTypographyVariants H4 { get; set; } = new();
    public BitThemeTypographyVariants H5 { get; set; } = new();
    public BitThemeTypographyVariants H6 { get; set; } = new();
    public BitThemeTypographyVariants Subtitle1 { get; set; } = new();
    public BitThemeTypographyVariants Subtitle2 { get; set; } = new();
    public BitThemeTypographyVariants Body1 { get; set; } = new();
    public BitThemeTypographyVariants Body2 { get; set; } = new();
    public BitThemeLabelTypographyVariants Button { get; set; } = new();
    public BitThemeTypographyVariants Caption1 { get; set; } = new();
    public BitThemeTypographyVariants Caption2 { get; set; } = new();
    public BitThemeLabelTypographyVariants Overline { get; set; } = new();
    public BitThemeInheritTypographyVariants Inherit { get; set; } = new();
}

/// <summary>
/// Common typography tokens shared by every text variant. Each property maps to a
/// <c>--bit-tpg-{variant}-{token}</c> CSS custom property (e.g. <c>--bit-tpg-h1-font-size</c>).
/// </summary>
/// <remarks>
/// Per-variant <c>font-family</c> is intentionally not exposed here: all variants inherit the
/// single root-level <see cref="BitThemeTypography.FontFamily"/> (<c>--bit-tpg-font-family</c>).
/// The only exception is <see cref="BitThemeTypography.Inherit"/>, which overrides it via
/// <see cref="BitThemeInheritTypographyVariants.FontFamily"/>.
/// </remarks>
public class BitThemeTypographyVariants
{
    public string? Margin { get; set; }
    public string? FontWeight { get; set; }
    public string? FontSize { get; set; }
    public string? LineHeight { get; set; }
    public string? LetterSpacing { get; set; }
}

/// <summary>
/// Label-style variants (<see cref="BitThemeTypography.Button"/>, <see cref="BitThemeTypography.Overline"/>,
/// and - via <see cref="BitThemeInheritTypographyVariants"/> - <see cref="BitThemeTypography.Inherit"/>)
/// that additionally control <c>text-transform</c> (<c>--bit-tpg-{variant}-text-transform</c>) and
/// <c>display</c> (<c>--bit-tpg-{variant}-display</c>).
/// </summary>
public class BitThemeLabelTypographyVariants : BitThemeTypographyVariants
{
    public string? TextTransform { get; set; }
    public string? Display { get; set; }
}

/// <summary>
/// The <see cref="BitThemeTypography.Inherit"/> variant, which additionally allows overriding
/// <c>font-family</c> (<c>--bit-tpg-inherit-font-family</c>). Every other variant inherits the
/// root-level <see cref="BitThemeTypography.FontFamily"/>. Deriving from
/// <see cref="BitThemeLabelTypographyVariants"/> also exposes <c>text-transform</c>
/// (<c>--bit-tpg-inherit-text-transform</c>) and <c>display</c> (<c>--bit-tpg-inherit-display</c>),
/// matching the packaged stylesheet, which declares both tokens for this variant.
/// </summary>
public class BitThemeInheritTypographyVariants : BitThemeLabelTypographyVariants
{
    public string? FontFamily { get; set; }
}

/// <summary>
/// The font-size scale (<c>--bit-tpg-fs-{2xs,xs,sm,md,lg,xl,2xl,3xl,4xl}</c>) every component reads its
/// text and glyph sizes from. Values are lengths in <c>rem</c> by default, so type follows the root font
/// size rather than the spacing unit or the density multiplier. Component size classes map
/// <c>sm</c> to <see cref="Xs"/>, <c>md</c> to <see cref="Sm"/> and <c>lg</c> to <see cref="Md"/>.
/// </summary>
public class BitThemeTypographyFontSizes
{
    /// <summary><c>--bit-tpg-fs-2xs</c> (10px by default).</summary>
    public string? Xxs { get; set; }

    /// <summary><c>--bit-tpg-fs-xs</c> (12px by default).</summary>
    public string? Xs { get; set; }

    /// <summary><c>--bit-tpg-fs-sm</c> (14px by default).</summary>
    public string? Sm { get; set; }

    /// <summary><c>--bit-tpg-fs-md</c> (16px by default).</summary>
    public string? Md { get; set; }

    /// <summary><c>--bit-tpg-fs-lg</c> (18px by default).</summary>
    public string? Lg { get; set; }

    /// <summary><c>--bit-tpg-fs-xl</c> (20px by default).</summary>
    public string? Xl { get; set; }

    /// <summary><c>--bit-tpg-fs-2xl</c> (24px by default).</summary>
    public string? Xxl { get; set; }

    /// <summary><c>--bit-tpg-fs-3xl</c> (28px by default).</summary>
    public string? Xxxl { get; set; }

    /// <summary><c>--bit-tpg-fs-4xl</c> (32px by default).</summary>
    public string? Xxxxl { get; set; }
}

/// <summary>
/// The font-weight scale (<c>--bit-tpg-fw-{light,regular,medium,semibold,bold}</c>). Components never
/// write a literal weight, so a design system that ships a different face retunes the ramp here.
/// </summary>
public class BitThemeTypographyFontWeights
{
    public string? Light { get; set; }
    public string? Regular { get; set; }
    public string? Medium { get; set; }
    public string? SemiBold { get; set; }
    public string? Bold { get; set; }
}

/// <summary>
/// The tracking and case of the label of an interactive control - buttons, tags, tabs, nav items
/// (<c>--bit-tpg-ctrl-letter-spacing</c>, <c>--bit-tpg-ctrl-text-transform</c>).
/// </summary>
public class BitThemeControlTypography
{
    public string? LetterSpacing { get; set; }
    public string? TextTransform { get; set; }
}
