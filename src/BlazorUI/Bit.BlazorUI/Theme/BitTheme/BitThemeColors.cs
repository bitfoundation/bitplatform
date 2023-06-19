namespace Bit.BlazorUI;

public class BitThemeColors
{
    public BitThemeColorVariants Primary { get; set; } = new();
    public BitThemeColorVariants Secondary { get; set; } = new();
    public BitThemeGeneralColorVariants Foreground { get; set; } = new();
    public BitThemeBackgroundColorVariants Background { get; set; } = new();
    public BitThemeGeneralColorVariants Border { get; set; } = new();
    public BitThemeActionColorVariants Action { get; set; } = new();
    public BitThemeStateColorVariants State { get; set; } = new();
    public BitThemeNeutralColorVariants Neutral { get; set; } = new();
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
    public BitThemeSubActionColorVariants Hover { get; set; } = new();
    public BitThemeSubActionColorVariants Active { get; set; } = new();
}
public class BitThemeSubActionColorVariants
{
    public string? Primary { get; set; }
    public string? PrimaryDark { get; set; }
    public string? PrimaryLight { get; set; }
    public string? Secondary { get; set; }
    public string? SecondaryDark { get; set; }
    public string? SecondaryLight { get; set; }

    public BitThemeSubColorVariants Foreground { get; set; } = new();
    public BitThemeSubColorVariants Background { get; set; } = new();
    public BitThemeSubColorVariants Border { get; set; } = new();
}
public class BitThemeSubColorVariants
{
    public string? Primary { get; set; }
    public string? Secondary { get; set; }
}


public class BitThemeStateColorVariants
{
    public string? Info { get; set; }
    public string? InfoBackground { get; set; }
    public string? Success { get; set; }
    public string? SuccessBackground { get; set; }
    public string? Warning { get; set; }
    public string? WarningBackground { get; set; }
    public string? SevereWarning { get; set; }
    public string? SevereWarningBackground { get; set; }
    public string? Error { get; set; }
    public string? ErrorBackground { get; set; }
    public string? Required { get; set; }
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
