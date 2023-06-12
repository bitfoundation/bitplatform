namespace Bit.BlazorUI;

public class BitTheme
{
    public BitThemeColors Colors { get; set; } = new BitThemeColors();
}


public class BitThemeColors
{
    public BitThemeColorVariants Primary { get; private set; } = new BitThemeColorVariants();
    public BitThemeColorVariants Secondary { get; private set; } = new BitThemeColorVariants();
    public BitThemeGeneralColorVariants Foreground { get; private set; } = new BitThemeGeneralColorVariants();
    public BitThemeBackgroundColorVariants Background { get; private set; } = new BitThemeBackgroundColorVariants();
    public BitThemeGeneralColorVariants Border { get; private set; } = new BitThemeGeneralColorVariants();
    public BitThemeActionColorVariants Action { get; private set; } = new BitThemeActionColorVariants();

}
public class BitThemeColorVariants
{
    public string? Main { get; set; }
    public string? Dark { get; set; }
    public string? Light { get; set; }
    public string? Text { get; set; }
}

public class BitThemeGeneralColorVariants
{
    public string? Primary { get; set; }
    public string? Secondary { get; set; }
    public string? Disabled { get; set; }
}

public class BitThemeBackgroundColorVariants : BitThemeGeneralColorVariants
{
    public string? Overlay { get; set; }
}

public class BitThemeActionColorVariants
{

}

public class BitThemeActionHoverColorVariants
{
    public string? Primary { get; set; }
    public string? PrimaryDark { get; set; }
    public string? PrimaryLight { get; set; }
    public string? Secondary { get; set; }
    public string? SecondaryDark { get; set; }
    public string? SecondaryLight { get; set; }
}
