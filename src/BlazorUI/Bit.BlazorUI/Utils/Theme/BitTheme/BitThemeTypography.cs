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
    public BitThemeTypographyVariants Button { get; set; } = new();
    public BitThemeTypographyVariants Caption1 { get; set; } = new();
    public BitThemeTypographyVariants Caption2 { get; set; } = new();
    public BitThemeTypographyVariants Overline { get; set; } = new();
    public BitThemeTypographyVariants Inherit { get; set; } = new();
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
