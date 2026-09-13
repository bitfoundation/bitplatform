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
    public BitThemeSemanticColors Semantic { get; set; } = new();
}

/// <summary>
/// The intent-level alias tier (<c>--bit-sem-*</c>): purpose-named tokens defined over the
/// primitive <c>--bit-clr-*</c> / <c>--bit-shd-*</c> tokens, for APP code (custom CSS, layout
/// chrome) that wants to express intent ("elevated surface") instead of a primitive name
/// (<c>--bit-clr-bg-sec</c>). Components deliberately consume primitives and per-role variables -
/// overriding a semantic token retunes app styling only, while overriding a primitive flows
/// through to both. Values default to <c>var(...)</c> references (see
/// <c>Styles/semantic-tokens.scss</c>), so they track whatever palette is active; set one here to
/// pin that intent to a specific value instead.
/// </summary>
public class BitThemeSemanticColors
{
    public string? SurfacePage { get; set; }
    public string? SurfaceElevated { get; set; }
    public string? SurfaceMuted { get; set; }
    public string? TextPrimary { get; set; }
    public string? TextSecondary { get; set; }
    public string? BorderDefault { get; set; }
    public string? AccentPrimary { get; set; }

    /// <summary>Holds a full <c>box-shadow</c> value (defaults to <c>var(--bit-shd-focus-ring)</c>), not a color.</summary>
    public string? FocusRing { get; set; }

    public string? FocusColor { get; set; }
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
    public string? Disabled { get; set; }
    public string? DisabledText { get; set; }
    public string? Focus { get; set; }
}

public class BitThemeGeneralColorVariants
{
    public string? Primary { get; set; }
    public string? PrimaryHover { get; set; }
    public string? PrimaryActive { get; set; }
    public string? PrimaryDark { get; set; }
    public string? PrimaryDarkHover { get; set; }
    public string? PrimaryDarkActive { get; set; }
    public string? PrimaryLight { get; set; }
    public string? PrimaryLightHover { get; set; }
    public string? PrimaryLightActive { get; set; }
    public string? PrimaryDisabled { get; set; }
    public string? PrimaryDisabledText { get; set; }
    public string? PrimaryFocus { get; set; }
    public string? Secondary { get; set; }
    public string? SecondaryHover { get; set; }
    public string? SecondaryActive { get; set; }
    public string? SecondaryDark { get; set; }
    public string? SecondaryDarkHover { get; set; }
    public string? SecondaryDarkActive { get; set; }
    public string? SecondaryLight { get; set; }
    public string? SecondaryLightHover { get; set; }
    public string? SecondaryLightActive { get; set; }
    public string? SecondaryDisabled { get; set; }
    public string? SecondaryDisabledText { get; set; }
    public string? SecondaryFocus { get; set; }
    public string? Tertiary { get; set; }
    public string? TertiaryHover { get; set; }
    public string? TertiaryActive { get; set; }
    public string? TertiaryDark { get; set; }
    public string? TertiaryDarkHover { get; set; }
    public string? TertiaryDarkActive { get; set; }
    public string? TertiaryLight { get; set; }
    public string? TertiaryLightHover { get; set; }
    public string? TertiaryLightActive { get; set; }
    public string? TertiaryDisabled { get; set; }
    public string? TertiaryDisabledText { get; set; }
    public string? TertiaryFocus { get; set; }
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
