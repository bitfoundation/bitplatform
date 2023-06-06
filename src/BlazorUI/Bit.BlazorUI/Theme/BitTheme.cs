namespace Bit.BlazorUI;

public class BitTheme
{
    public BitThemeColors Colors { get; set; } = new BitThemeColors();
}


public class BitThemeColors
{
    public BitThemeColorVariants Primary { get; set; } = new BitThemeColorVariants();
    public BitThemeColorVariants Secondary { get; set; } = new BitThemeColorVariants();
}
public class BitThemeColorVariants
{
    public string? Main { get; set; }
    public string? High { get; set; }
    public string? Higher { get; set; }
    public string? Low { get; set; }
    public string? Lower { get; set; }
}
