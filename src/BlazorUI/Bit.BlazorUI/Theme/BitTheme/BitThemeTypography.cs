namespace Bit.BlazorUI;

public class BitThemeTypography
{
    public string? FontFamily { get; set; }
    public string? FontWeight { get; set; }
    public string? LineHeight { get; set; }
    public string? GutterSize { get; set; }

    public BitThemeTypographyVariants Body1 { get; private set; } = new();
    public BitThemeTypographyVariants Body2 { get; private set; } = new();
    public BitThemeTypographyVariants Button { get; private set; } = new();
    public BitThemeTypographyVariants Caption { get; private set; } = new();
    public BitThemeTypographyVariants H1 { get; private set; } = new();
    public BitThemeTypographyVariants H2 { get; private set; } = new();
    public BitThemeTypographyVariants H3 { get; private set; } = new();
    public BitThemeTypographyVariants H4 { get; private set; } = new();
    public BitThemeTypographyVariants H5 { get; private set; } = new();
    public BitThemeTypographyVariants H6 { get; private set; } = new();
    public BitThemeTypographyVariants Inherit { get; private set; } = new();
    public BitThemeTypographyVariants Overline { get; private set; } = new();
    public BitThemeTypographyVariants Subtitle1 { get; private set; } = new();
    public BitThemeTypographyVariants Subtitle2 { get; private set; } = new();
}

public class BitThemeTypographyVariants
{
    public string? Margin { get; set; }
    public string? FontFamily { get; set; }
    public string? FontWeight { get; set; }
    public string? FontSize { get; set; }
    public string? LineHeight { get; set; }
    public string? LetterSpacing { get; set; }
    public string? TextTransform { get; set; }
    public string? Display { get; set; }
}
