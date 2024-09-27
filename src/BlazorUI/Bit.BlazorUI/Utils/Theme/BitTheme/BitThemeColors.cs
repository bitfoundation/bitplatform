namespace Bit.BlazorUI;

public class BitThemeColors
{
    public BitThemeColorVariants Primary { get; set; } = new();
    public BitThemeColorVariants Secondary { get; set; } = new();
    public BitThemeColorVariants Tertiary { get; set; } = new();
    public BitThemeColorVariants Info { get; set; } = new();
    public BitThemeColorVariants Success { get; set; } = new();
    public BitThemeColorVariants Warning { get; set; } = new();
    public BitThemeColorVariants SevereWarning { get; set; } = new();
    public BitThemeColorVariants Error { get; set; } = new();
    public BitThemeGeneralColorVariants Foreground { get; set; } = new();
    public BitThemeBackgroundColorVariants Background { get; set; } = new();
    public BitThemeGeneralColorVariants Border { get; set; } = new();
    public string? Required { get; set; }
    public BitThemeNeutralColorVariants Neutral { get; set; } = new();
}

public class BitThemeColorVariants
{
    public string? Main { get; set; }
    public string? MainHover { get; set; }
    public string? MainActive { get; set; }
    public string? Dark { get; set; }
    public string? DarkHover { get; set; }
    public string? DarkActive { get; set; }
    public string? Light { get; set; }
    public string? LightHover { get; set; }
    public string? LightActive { get; set; }
    public string? Text { get; set; }
}

public class BitThemeGeneralColorVariants
{
    public string? Primary { get; set; }
    public string? PrimaryHover { get; set; }
    public string? PrimaryActive { get; set; }
    public string? Secondary { get; set; }
    public string? SecondaryHover { get; set; }
    public string? SecondaryActive { get; set; }
    public string? Tertiary { get; set; }
    public string? TertiaryHover { get; set; }
    public string? TertiaryActive { get; set; }
    public string? Disabled { get; set; }
}

public class BitThemeBackgroundColorVariants : BitThemeGeneralColorVariants
{
    public string? Overlay { get; set; }
}

public class BitThemeNeutralColorVariants
{
    public string? White { get; set; }
    public string? Black { get; set; }
    public string? Gray10 { get; set; }
    public string? Gray20 { get; set; }
    public string? Gray30 { get; set; }
    public string? Gray40 { get; set; }
    public string? Gray50 { get; set; }
    public string? Gray60 { get; set; }
    public string? Gray70 { get; set; }
    public string? Gray80 { get; set; }
    public string? Gray90 { get; set; }
    public string? Gray100 { get; set; }
    public string? Gray110 { get; set; }
    public string? Gray120 { get; set; }
    public string? Gray130 { get; set; }
    public string? Gray140 { get; set; }
    public string? Gray150 { get; set; }
    public string? Gray160 { get; set; }
    public string? Gray170 { get; set; }
    public string? Gray180 { get; set; }
    public string? Gray190 { get; set; }
    public string? Gray200 { get; set; }
    public string? Gray210 { get; set; }
    public string? Gray220 { get; set; }
}
