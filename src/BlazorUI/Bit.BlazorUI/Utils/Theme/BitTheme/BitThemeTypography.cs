namespace Bit.BlazorUI;

public class BitThemeTypography
{
    public string? FontFamily { get; set; }
    public string? FontWeight { get; set; }
    public string? LineHeight { get; set; }
    public string? GutterSize { get; set; }

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
