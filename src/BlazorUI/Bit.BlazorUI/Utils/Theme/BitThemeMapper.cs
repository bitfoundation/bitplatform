namespace Bit.BlazorUI;

internal static class BitThemeMapper
{
    /// <summary>
    /// The semantic alias tier's default targets, mirroring <c>Styles/semantic-tokens.scss</c>
    /// (pinned to it by a contract test). Used by <see cref="AugmentWithSemanticAliasReSubstitution"/>.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> SemanticAliasTargets = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [BitCss.Var.Semantic.SurfacePage] = BitCss.Var.Color.Background.Primary.Main,
        [BitCss.Var.Semantic.SurfaceElevated] = BitCss.Var.Color.Background.Secondary.Main,
        [BitCss.Var.Semantic.SurfaceMuted] = BitCss.Var.Color.Background.Tertiary.Main,
        [BitCss.Var.Semantic.TextPrimary] = BitCss.Var.Color.Foreground.Primary.Main,
        [BitCss.Var.Semantic.TextSecondary] = BitCss.Var.Color.Foreground.Secondary.Main,
        [BitCss.Var.Semantic.BorderDefault] = BitCss.Var.Color.Border.Primary.Main,
        [BitCss.Var.Semantic.AccentPrimary] = BitCss.Var.Color.Primary.Main,
        [BitCss.Var.Semantic.FocusRing] = BitCss.Var.Shadow.FocusRing,
        [BitCss.Var.Semantic.FocusColor] = BitCss.Var.Color.Primary.Focus,
    };

    /// <summary>
    /// Re-declares a semantic alias (as its default <c>var()</c> reference) next to any primitive
    /// the mapped theme overrides, so the alias tracks the override for the styled subtree.
    /// </summary>
    /// <remarks>
    /// This is required by how CSS custom properties compute: an alias's <c>var()</c> reference is
    /// substituted at the element that DEFINES the alias (<c>:root</c>, via
    /// <c>semantic-tokens.scss</c>), and descendants inherit the already-substituted value. Inline
    /// overrides applied lower in the tree (a <see cref="BitThemeProvider"/> wrapper, or
    /// <see cref="BitThemeManager.ApplyBitThemeAsync"/> on <c>document.body</c> / a target element)
    /// would therefore retune components - which read primitives - while app CSS reading the alias
    /// kept the document palette's stale value. Re-declaring the alias on the same element re-runs
    /// the substitution against the overridden primitive. Only aliases whose target primitive the
    /// theme actually sets are re-declared (a sparse overlay must not clobber unrelated,
    /// possibly-customized intents), and an alias the theme sets explicitly always wins.
    /// </remarks>
    internal static void AugmentWithSemanticAliasReSubstitution(Dictionary<string, string> cssVariables)
    {
        foreach (var (alias, target) in SemanticAliasTargets)
        {
            if (cssVariables.ContainsKey(alias)) continue; // explicit alias value wins
            if (cssVariables.ContainsKey(target) is false) continue; // primitive untouched; keep the inherited alias

            cssVariables[alias] = $"var({target})";
        }
    }

    /// <summary>
    /// The family alias tier's default targets, mirroring <c>Styles/family-tokens.scss</c> (pinned
    /// to it by a contract test). ORDERED so that a chained alias resolves in a single pass: an
    /// entry may only point at a primitive or at an alias declared ABOVE it (the control radius is
    /// re-declared before the button/chip/selection radii that fall back to it).
    /// </summary>
    /// <remarks>
    /// The app-bar shadows and the snackbar elevation are deliberately absent: they are not plain
    /// <c>var()</c> aliases of a primitive (a tinted expression and a literal <c>none</c>), so there
    /// is nothing to re-substitute for them.
    /// </remarks>
    internal static readonly IReadOnlyList<KeyValuePair<string, string>> FamilyAliasTargets =
    [
        new(BitCss.Var.Shape.Radius.Control, BitCss.Var.Shape.BorderRadius),
        new(BitCss.Var.Shape.Radius.Surface, BitCss.Var.Shape.BorderRadius),
        new(BitCss.Var.Shape.Radius.Popup, BitCss.Var.Shape.BorderRadius),
        new(BitCss.Var.Shape.Radius.Dialog, BitCss.Var.Shape.BorderRadius),
        new(BitCss.Var.Shape.Radius.Button, BitCss.Var.Shape.Radius.Control),
        new(BitCss.Var.Shape.Radius.Chip, BitCss.Var.Shape.Radius.Control),
        new(BitCss.Var.Shape.Radius.Selection, BitCss.Var.Shape.Radius.Control),
        new(BitCss.Var.Shadow.Card, BitCss.Var.Shadow.Callout),
        new(BitCss.Var.Shadow.Popup, BitCss.Var.Shadow.Callout),
        new(BitCss.Var.Shadow.Dialog, BitCss.Var.Shadow.Callout),
        new(BitCss.Var.Shadow.Sheet, BitCss.Var.Shadow.Callout),
        new(BitCss.Var.Shadow.Tooltip, BitCss.Var.Shadow.Callout),
    ];

    /// <summary>
    /// Re-declares a family alias (as its default <c>var()</c> reference) next to any token the
    /// mapped theme overrides it from, so the components - which read the family tier - track an
    /// inline override for the styled subtree.
    /// </summary>
    /// <remarks>
    /// The same substitution rule as <see cref="AugmentWithSemanticAliasReSubstitution"/>, one tier
    /// lower: <c>family-tokens.scss</c> declares the family aliases on <c>:root</c>, so a theme
    /// applied further down the tree (a <see cref="BitThemeProvider"/> wrapper, or
    /// <see cref="BitThemeManager.ApplyBitThemeAsync"/> on an element) that re-values
    /// <c>--bit-shp-brd-radius</c> or <c>--bit-shd-cal</c> would otherwise leave every component
    /// reading the document's already-substituted family value. The table is walked in order, so an
    /// alias re-declared here becomes a touched target for the aliases that fall back to IT - which
    /// is what carries a <c>Radius.Control</c> override through to buttons, chips and checkboxes.
    /// An alias the theme sets explicitly always wins.
    /// </remarks>
    internal static void AugmentWithFamilyAliasReSubstitution(Dictionary<string, string> cssVariables)
    {
        foreach (var (alias, target) in FamilyAliasTargets)
        {
            if (cssVariables.ContainsKey(alias)) continue; // explicit alias value wins
            if (cssVariables.ContainsKey(target) is false) continue; // target untouched; keep the inherited alias

            cssVariables[alias] = $"var({target})";
        }
    }

    internal static Dictionary<string, string> MapToCssVariables(BitTheme bitTheme)
    {
        var result = new Dictionary<string, string>();

        if (bitTheme is null) return result;

        // Walk a normalized COPY so a hand-constructed sparse theme (e.g. new BitTheme { Color = null }
        // or new BitThemeColors { Primary = null }, both reachable via the public setters) can be
        // traversed without NRE - WITHOUT mutating the caller's instance. (Previously this filled
        // null branch objects in place on the passed theme as a side effect.)
        bitTheme = NormalizeToNew(bitTheme);

        addCssVar(BitCss.Var.Color.Primary.Main, bitTheme.Color.Primary.Main);
        addCssVar(BitCss.Var.Color.Primary.Hover.Main, bitTheme.Color.Primary.MainHover);
        addCssVar(BitCss.Var.Color.Primary.Active.Main, bitTheme.Color.Primary.MainActive);
        addCssVar(BitCss.Var.Color.Primary.Dark, bitTheme.Color.Primary.Dark);
        addCssVar(BitCss.Var.Color.Primary.Hover.Dark, bitTheme.Color.Primary.DarkHover);
        addCssVar(BitCss.Var.Color.Primary.Active.Dark, bitTheme.Color.Primary.DarkActive);
        addCssVar(BitCss.Var.Color.Primary.Light, bitTheme.Color.Primary.Light);
        addCssVar(BitCss.Var.Color.Primary.Hover.Light, bitTheme.Color.Primary.LightHover);
        addCssVar(BitCss.Var.Color.Primary.Active.Light, bitTheme.Color.Primary.LightActive);
        addCssVar(BitCss.Var.Color.Primary.Text, bitTheme.Color.Primary.Text);
        addCssVar(BitCss.Var.Color.Primary.Disabled, bitTheme.Color.Primary.Disabled);
        addCssVar(BitCss.Var.Color.Primary.DisabledText, bitTheme.Color.Primary.DisabledText);
        addCssVar(BitCss.Var.Color.Primary.Focus, bitTheme.Color.Primary.Focus);

        addCssVar(BitCss.Var.Color.Secondary.Main, bitTheme.Color.Secondary.Main);
        addCssVar(BitCss.Var.Color.Secondary.Hover.Main, bitTheme.Color.Secondary.MainHover);
        addCssVar(BitCss.Var.Color.Secondary.Active.Main, bitTheme.Color.Secondary.MainActive);
        addCssVar(BitCss.Var.Color.Secondary.Dark, bitTheme.Color.Secondary.Dark);
        addCssVar(BitCss.Var.Color.Secondary.Hover.Dark, bitTheme.Color.Secondary.DarkHover);
        addCssVar(BitCss.Var.Color.Secondary.Active.Dark, bitTheme.Color.Secondary.DarkActive);
        addCssVar(BitCss.Var.Color.Secondary.Light, bitTheme.Color.Secondary.Light);
        addCssVar(BitCss.Var.Color.Secondary.Hover.Light, bitTheme.Color.Secondary.LightHover);
        addCssVar(BitCss.Var.Color.Secondary.Active.Light, bitTheme.Color.Secondary.LightActive);
        addCssVar(BitCss.Var.Color.Secondary.Text, bitTheme.Color.Secondary.Text);
        addCssVar(BitCss.Var.Color.Secondary.Disabled, bitTheme.Color.Secondary.Disabled);
        addCssVar(BitCss.Var.Color.Secondary.DisabledText, bitTheme.Color.Secondary.DisabledText);
        addCssVar(BitCss.Var.Color.Secondary.Focus, bitTheme.Color.Secondary.Focus);

        addCssVar(BitCss.Var.Color.Tertiary.Main, bitTheme.Color.Tertiary.Main);
        addCssVar(BitCss.Var.Color.Tertiary.Hover.Main, bitTheme.Color.Tertiary.MainHover);
        addCssVar(BitCss.Var.Color.Tertiary.Active.Main, bitTheme.Color.Tertiary.MainActive);
        addCssVar(BitCss.Var.Color.Tertiary.Dark, bitTheme.Color.Tertiary.Dark);
        addCssVar(BitCss.Var.Color.Tertiary.Hover.Dark, bitTheme.Color.Tertiary.DarkHover);
        addCssVar(BitCss.Var.Color.Tertiary.Active.Dark, bitTheme.Color.Tertiary.DarkActive);
        addCssVar(BitCss.Var.Color.Tertiary.Light, bitTheme.Color.Tertiary.Light);
        addCssVar(BitCss.Var.Color.Tertiary.Hover.Light, bitTheme.Color.Tertiary.LightHover);
        addCssVar(BitCss.Var.Color.Tertiary.Active.Light, bitTheme.Color.Tertiary.LightActive);
        addCssVar(BitCss.Var.Color.Tertiary.Text, bitTheme.Color.Tertiary.Text);
        addCssVar(BitCss.Var.Color.Tertiary.Disabled, bitTheme.Color.Tertiary.Disabled);
        addCssVar(BitCss.Var.Color.Tertiary.DisabledText, bitTheme.Color.Tertiary.DisabledText);
        addCssVar(BitCss.Var.Color.Tertiary.Focus, bitTheme.Color.Tertiary.Focus);

        addCssVar(BitCss.Var.Color.Info.Main, bitTheme.Color.Info.Main);
        addCssVar(BitCss.Var.Color.Info.Hover.Main, bitTheme.Color.Info.MainHover);
        addCssVar(BitCss.Var.Color.Info.Active.Main, bitTheme.Color.Info.MainActive);
        addCssVar(BitCss.Var.Color.Info.Dark, bitTheme.Color.Info.Dark);
        addCssVar(BitCss.Var.Color.Info.Hover.Dark, bitTheme.Color.Info.DarkHover);
        addCssVar(BitCss.Var.Color.Info.Active.Dark, bitTheme.Color.Info.DarkActive);
        addCssVar(BitCss.Var.Color.Info.Light, bitTheme.Color.Info.Light);
        addCssVar(BitCss.Var.Color.Info.Hover.Light, bitTheme.Color.Info.LightHover);
        addCssVar(BitCss.Var.Color.Info.Active.Light, bitTheme.Color.Info.LightActive);
        addCssVar(BitCss.Var.Color.Info.Text, bitTheme.Color.Info.Text);
        addCssVar(BitCss.Var.Color.Info.Disabled, bitTheme.Color.Info.Disabled);
        addCssVar(BitCss.Var.Color.Info.DisabledText, bitTheme.Color.Info.DisabledText);
        addCssVar(BitCss.Var.Color.Info.Focus, bitTheme.Color.Info.Focus);

        addCssVar(BitCss.Var.Color.Success.Main, bitTheme.Color.Success.Main);
        addCssVar(BitCss.Var.Color.Success.Hover.Main, bitTheme.Color.Success.MainHover);
        addCssVar(BitCss.Var.Color.Success.Active.Main, bitTheme.Color.Success.MainActive);
        addCssVar(BitCss.Var.Color.Success.Dark, bitTheme.Color.Success.Dark);
        addCssVar(BitCss.Var.Color.Success.Hover.Dark, bitTheme.Color.Success.DarkHover);
        addCssVar(BitCss.Var.Color.Success.Active.Dark, bitTheme.Color.Success.DarkActive);
        addCssVar(BitCss.Var.Color.Success.Light, bitTheme.Color.Success.Light);
        addCssVar(BitCss.Var.Color.Success.Hover.Light, bitTheme.Color.Success.LightHover);
        addCssVar(BitCss.Var.Color.Success.Active.Light, bitTheme.Color.Success.LightActive);
        addCssVar(BitCss.Var.Color.Success.Text, bitTheme.Color.Success.Text);
        addCssVar(BitCss.Var.Color.Success.Disabled, bitTheme.Color.Success.Disabled);
        addCssVar(BitCss.Var.Color.Success.DisabledText, bitTheme.Color.Success.DisabledText);
        addCssVar(BitCss.Var.Color.Success.Focus, bitTheme.Color.Success.Focus);

        addCssVar(BitCss.Var.Color.Warning.Main, bitTheme.Color.Warning.Main);
        addCssVar(BitCss.Var.Color.Warning.Hover.Main, bitTheme.Color.Warning.MainHover);
        addCssVar(BitCss.Var.Color.Warning.Active.Main, bitTheme.Color.Warning.MainActive);
        addCssVar(BitCss.Var.Color.Warning.Dark, bitTheme.Color.Warning.Dark);
        addCssVar(BitCss.Var.Color.Warning.Hover.Dark, bitTheme.Color.Warning.DarkHover);
        addCssVar(BitCss.Var.Color.Warning.Active.Dark, bitTheme.Color.Warning.DarkActive);
        addCssVar(BitCss.Var.Color.Warning.Light, bitTheme.Color.Warning.Light);
        addCssVar(BitCss.Var.Color.Warning.Hover.Light, bitTheme.Color.Warning.LightHover);
        addCssVar(BitCss.Var.Color.Warning.Active.Light, bitTheme.Color.Warning.LightActive);
        addCssVar(BitCss.Var.Color.Warning.Text, bitTheme.Color.Warning.Text);
        addCssVar(BitCss.Var.Color.Warning.Disabled, bitTheme.Color.Warning.Disabled);
        addCssVar(BitCss.Var.Color.Warning.DisabledText, bitTheme.Color.Warning.DisabledText);
        addCssVar(BitCss.Var.Color.Warning.Focus, bitTheme.Color.Warning.Focus);

        addCssVar(BitCss.Var.Color.SevereWarning.Main, bitTheme.Color.SevereWarning.Main);
        addCssVar(BitCss.Var.Color.SevereWarning.Hover.Main, bitTheme.Color.SevereWarning.MainHover);
        addCssVar(BitCss.Var.Color.SevereWarning.Active.Main, bitTheme.Color.SevereWarning.MainActive);
        addCssVar(BitCss.Var.Color.SevereWarning.Dark, bitTheme.Color.SevereWarning.Dark);
        addCssVar(BitCss.Var.Color.SevereWarning.Hover.Dark, bitTheme.Color.SevereWarning.DarkHover);
        addCssVar(BitCss.Var.Color.SevereWarning.Active.Dark, bitTheme.Color.SevereWarning.DarkActive);
        addCssVar(BitCss.Var.Color.SevereWarning.Light, bitTheme.Color.SevereWarning.Light);
        addCssVar(BitCss.Var.Color.SevereWarning.Hover.Light, bitTheme.Color.SevereWarning.LightHover);
        addCssVar(BitCss.Var.Color.SevereWarning.Active.Light, bitTheme.Color.SevereWarning.LightActive);
        addCssVar(BitCss.Var.Color.SevereWarning.Text, bitTheme.Color.SevereWarning.Text);
        addCssVar(BitCss.Var.Color.SevereWarning.Disabled, bitTheme.Color.SevereWarning.Disabled);
        addCssVar(BitCss.Var.Color.SevereWarning.DisabledText, bitTheme.Color.SevereWarning.DisabledText);
        addCssVar(BitCss.Var.Color.SevereWarning.Focus, bitTheme.Color.SevereWarning.Focus);

        addCssVar(BitCss.Var.Color.Error.Main, bitTheme.Color.Error.Main);
        addCssVar(BitCss.Var.Color.Error.Hover.Main, bitTheme.Color.Error.MainHover);
        addCssVar(BitCss.Var.Color.Error.Active.Main, bitTheme.Color.Error.MainActive);
        addCssVar(BitCss.Var.Color.Error.Dark, bitTheme.Color.Error.Dark);
        addCssVar(BitCss.Var.Color.Error.Hover.Dark, bitTheme.Color.Error.DarkHover);
        addCssVar(BitCss.Var.Color.Error.Active.Dark, bitTheme.Color.Error.DarkActive);
        addCssVar(BitCss.Var.Color.Error.Light, bitTheme.Color.Error.Light);
        addCssVar(BitCss.Var.Color.Error.Hover.Light, bitTheme.Color.Error.LightHover);
        addCssVar(BitCss.Var.Color.Error.Active.Light, bitTheme.Color.Error.LightActive);
        addCssVar(BitCss.Var.Color.Error.Text, bitTheme.Color.Error.Text);
        addCssVar(BitCss.Var.Color.Error.Disabled, bitTheme.Color.Error.Disabled);
        addCssVar(BitCss.Var.Color.Error.DisabledText, bitTheme.Color.Error.DisabledText);
        addCssVar(BitCss.Var.Color.Error.Focus, bitTheme.Color.Error.Focus);

        addCssVar(BitCss.Var.Color.Foreground.Primary.Main, bitTheme.Color.Foreground.Primary);
        addCssVar(BitCss.Var.Color.Foreground.Primary.Hover.Main, bitTheme.Color.Foreground.PrimaryHover);
        addCssVar(BitCss.Var.Color.Foreground.Primary.Active.Main, bitTheme.Color.Foreground.PrimaryActive);
        addCssVar(BitCss.Var.Color.Foreground.Primary.Dark, bitTheme.Color.Foreground.PrimaryDark);
        addCssVar(BitCss.Var.Color.Foreground.Primary.Hover.Dark, bitTheme.Color.Foreground.PrimaryDarkHover);
        addCssVar(BitCss.Var.Color.Foreground.Primary.Active.Dark, bitTheme.Color.Foreground.PrimaryDarkActive);
        addCssVar(BitCss.Var.Color.Foreground.Primary.Light, bitTheme.Color.Foreground.PrimaryLight);
        addCssVar(BitCss.Var.Color.Foreground.Primary.Hover.Light, bitTheme.Color.Foreground.PrimaryLightHover);
        addCssVar(BitCss.Var.Color.Foreground.Primary.Active.Light, bitTheme.Color.Foreground.PrimaryLightActive);
        addCssVar(BitCss.Var.Color.Foreground.Primary.Disabled, bitTheme.Color.Foreground.PrimaryDisabled);
        addCssVar(BitCss.Var.Color.Foreground.Primary.DisabledText, bitTheme.Color.Foreground.PrimaryDisabledText);
        addCssVar(BitCss.Var.Color.Foreground.Primary.Focus, bitTheme.Color.Foreground.PrimaryFocus);
        addCssVar(BitCss.Var.Color.Foreground.Secondary.Main, bitTheme.Color.Foreground.Secondary);
        addCssVar(BitCss.Var.Color.Foreground.Secondary.Hover.Main, bitTheme.Color.Foreground.SecondaryHover);
        addCssVar(BitCss.Var.Color.Foreground.Secondary.Active.Main, bitTheme.Color.Foreground.SecondaryActive);
        addCssVar(BitCss.Var.Color.Foreground.Secondary.Dark, bitTheme.Color.Foreground.SecondaryDark);
        addCssVar(BitCss.Var.Color.Foreground.Secondary.Hover.Dark, bitTheme.Color.Foreground.SecondaryDarkHover);
        addCssVar(BitCss.Var.Color.Foreground.Secondary.Active.Dark, bitTheme.Color.Foreground.SecondaryDarkActive);
        addCssVar(BitCss.Var.Color.Foreground.Secondary.Light, bitTheme.Color.Foreground.SecondaryLight);
        addCssVar(BitCss.Var.Color.Foreground.Secondary.Hover.Light, bitTheme.Color.Foreground.SecondaryLightHover);
        addCssVar(BitCss.Var.Color.Foreground.Secondary.Active.Light, bitTheme.Color.Foreground.SecondaryLightActive);
        addCssVar(BitCss.Var.Color.Foreground.Secondary.Disabled, bitTheme.Color.Foreground.SecondaryDisabled);
        addCssVar(BitCss.Var.Color.Foreground.Secondary.DisabledText, bitTheme.Color.Foreground.SecondaryDisabledText);
        addCssVar(BitCss.Var.Color.Foreground.Secondary.Focus, bitTheme.Color.Foreground.SecondaryFocus);
        addCssVar(BitCss.Var.Color.Foreground.Tertiary.Main, bitTheme.Color.Foreground.Tertiary);
        addCssVar(BitCss.Var.Color.Foreground.Tertiary.Hover.Main, bitTheme.Color.Foreground.TertiaryHover);
        addCssVar(BitCss.Var.Color.Foreground.Tertiary.Active.Main, bitTheme.Color.Foreground.TertiaryActive);
        addCssVar(BitCss.Var.Color.Foreground.Tertiary.Dark, bitTheme.Color.Foreground.TertiaryDark);
        addCssVar(BitCss.Var.Color.Foreground.Tertiary.Hover.Dark, bitTheme.Color.Foreground.TertiaryDarkHover);
        addCssVar(BitCss.Var.Color.Foreground.Tertiary.Active.Dark, bitTheme.Color.Foreground.TertiaryDarkActive);
        addCssVar(BitCss.Var.Color.Foreground.Tertiary.Light, bitTheme.Color.Foreground.TertiaryLight);
        addCssVar(BitCss.Var.Color.Foreground.Tertiary.Hover.Light, bitTheme.Color.Foreground.TertiaryLightHover);
        addCssVar(BitCss.Var.Color.Foreground.Tertiary.Active.Light, bitTheme.Color.Foreground.TertiaryLightActive);
        addCssVar(BitCss.Var.Color.Foreground.Tertiary.Disabled, bitTheme.Color.Foreground.TertiaryDisabled);
        addCssVar(BitCss.Var.Color.Foreground.Tertiary.DisabledText, bitTheme.Color.Foreground.TertiaryDisabledText);
        addCssVar(BitCss.Var.Color.Foreground.Tertiary.Focus, bitTheme.Color.Foreground.TertiaryFocus);
        addCssVar(BitCss.Var.Color.Foreground.Disabled, bitTheme.Color.Foreground.Disabled);

        addCssVar(BitCss.Var.Color.Background.Primary.Main, bitTheme.Color.Background.Primary);
        addCssVar(BitCss.Var.Color.Background.Primary.Hover.Main, bitTheme.Color.Background.PrimaryHover);
        addCssVar(BitCss.Var.Color.Background.Primary.Active.Main, bitTheme.Color.Background.PrimaryActive);
        addCssVar(BitCss.Var.Color.Background.Primary.Dark, bitTheme.Color.Background.PrimaryDark);
        addCssVar(BitCss.Var.Color.Background.Primary.Hover.Dark, bitTheme.Color.Background.PrimaryDarkHover);
        addCssVar(BitCss.Var.Color.Background.Primary.Active.Dark, bitTheme.Color.Background.PrimaryDarkActive);
        addCssVar(BitCss.Var.Color.Background.Primary.Light, bitTheme.Color.Background.PrimaryLight);
        addCssVar(BitCss.Var.Color.Background.Primary.Hover.Light, bitTheme.Color.Background.PrimaryLightHover);
        addCssVar(BitCss.Var.Color.Background.Primary.Active.Light, bitTheme.Color.Background.PrimaryLightActive);
        addCssVar(BitCss.Var.Color.Background.Primary.Disabled, bitTheme.Color.Background.PrimaryDisabled);
        addCssVar(BitCss.Var.Color.Background.Primary.DisabledText, bitTheme.Color.Background.PrimaryDisabledText);
        addCssVar(BitCss.Var.Color.Background.Primary.Focus, bitTheme.Color.Background.PrimaryFocus);
        addCssVar(BitCss.Var.Color.Background.Secondary.Main, bitTheme.Color.Background.Secondary);
        addCssVar(BitCss.Var.Color.Background.Secondary.Hover.Main, bitTheme.Color.Background.SecondaryHover);
        addCssVar(BitCss.Var.Color.Background.Secondary.Active.Main, bitTheme.Color.Background.SecondaryActive);
        addCssVar(BitCss.Var.Color.Background.Secondary.Dark, bitTheme.Color.Background.SecondaryDark);
        addCssVar(BitCss.Var.Color.Background.Secondary.Hover.Dark, bitTheme.Color.Background.SecondaryDarkHover);
        addCssVar(BitCss.Var.Color.Background.Secondary.Active.Dark, bitTheme.Color.Background.SecondaryDarkActive);
        addCssVar(BitCss.Var.Color.Background.Secondary.Light, bitTheme.Color.Background.SecondaryLight);
        addCssVar(BitCss.Var.Color.Background.Secondary.Hover.Light, bitTheme.Color.Background.SecondaryLightHover);
        addCssVar(BitCss.Var.Color.Background.Secondary.Active.Light, bitTheme.Color.Background.SecondaryLightActive);
        addCssVar(BitCss.Var.Color.Background.Secondary.Disabled, bitTheme.Color.Background.SecondaryDisabled);
        addCssVar(BitCss.Var.Color.Background.Secondary.DisabledText, bitTheme.Color.Background.SecondaryDisabledText);
        addCssVar(BitCss.Var.Color.Background.Secondary.Focus, bitTheme.Color.Background.SecondaryFocus);
        addCssVar(BitCss.Var.Color.Background.Tertiary.Main, bitTheme.Color.Background.Tertiary);
        addCssVar(BitCss.Var.Color.Background.Tertiary.Hover.Main, bitTheme.Color.Background.TertiaryHover);
        addCssVar(BitCss.Var.Color.Background.Tertiary.Active.Main, bitTheme.Color.Background.TertiaryActive);
        addCssVar(BitCss.Var.Color.Background.Tertiary.Dark, bitTheme.Color.Background.TertiaryDark);
        addCssVar(BitCss.Var.Color.Background.Tertiary.Hover.Dark, bitTheme.Color.Background.TertiaryDarkHover);
        addCssVar(BitCss.Var.Color.Background.Tertiary.Active.Dark, bitTheme.Color.Background.TertiaryDarkActive);
        addCssVar(BitCss.Var.Color.Background.Tertiary.Light, bitTheme.Color.Background.TertiaryLight);
        addCssVar(BitCss.Var.Color.Background.Tertiary.Hover.Light, bitTheme.Color.Background.TertiaryLightHover);
        addCssVar(BitCss.Var.Color.Background.Tertiary.Active.Light, bitTheme.Color.Background.TertiaryLightActive);
        addCssVar(BitCss.Var.Color.Background.Tertiary.Disabled, bitTheme.Color.Background.TertiaryDisabled);
        addCssVar(BitCss.Var.Color.Background.Tertiary.DisabledText, bitTheme.Color.Background.TertiaryDisabledText);
        addCssVar(BitCss.Var.Color.Background.Tertiary.Focus, bitTheme.Color.Background.TertiaryFocus);
        addCssVar(BitCss.Var.Color.Background.Disabled, bitTheme.Color.Background.Disabled);
        addCssVar(BitCss.Var.Color.Background.Overlay, bitTheme.Color.Background.Overlay);

        addCssVar(BitCss.Var.Color.Border.Primary.Main, bitTheme.Color.Border.Primary);
        addCssVar(BitCss.Var.Color.Border.Primary.Hover.Main, bitTheme.Color.Border.PrimaryHover);
        addCssVar(BitCss.Var.Color.Border.Primary.Active.Main, bitTheme.Color.Border.PrimaryActive);
        addCssVar(BitCss.Var.Color.Border.Primary.Dark, bitTheme.Color.Border.PrimaryDark);
        addCssVar(BitCss.Var.Color.Border.Primary.Hover.Dark, bitTheme.Color.Border.PrimaryDarkHover);
        addCssVar(BitCss.Var.Color.Border.Primary.Active.Dark, bitTheme.Color.Border.PrimaryDarkActive);
        addCssVar(BitCss.Var.Color.Border.Primary.Light, bitTheme.Color.Border.PrimaryLight);
        addCssVar(BitCss.Var.Color.Border.Primary.Hover.Light, bitTheme.Color.Border.PrimaryLightHover);
        addCssVar(BitCss.Var.Color.Border.Primary.Active.Light, bitTheme.Color.Border.PrimaryLightActive);
        addCssVar(BitCss.Var.Color.Border.Primary.Disabled, bitTheme.Color.Border.PrimaryDisabled);
        addCssVar(BitCss.Var.Color.Border.Primary.DisabledText, bitTheme.Color.Border.PrimaryDisabledText);
        addCssVar(BitCss.Var.Color.Border.Primary.Focus, bitTheme.Color.Border.PrimaryFocus);
        addCssVar(BitCss.Var.Color.Border.Secondary.Main, bitTheme.Color.Border.Secondary);
        addCssVar(BitCss.Var.Color.Border.Secondary.Hover.Main, bitTheme.Color.Border.SecondaryHover);
        addCssVar(BitCss.Var.Color.Border.Secondary.Active.Main, bitTheme.Color.Border.SecondaryActive);
        addCssVar(BitCss.Var.Color.Border.Secondary.Dark, bitTheme.Color.Border.SecondaryDark);
        addCssVar(BitCss.Var.Color.Border.Secondary.Hover.Dark, bitTheme.Color.Border.SecondaryDarkHover);
        addCssVar(BitCss.Var.Color.Border.Secondary.Active.Dark, bitTheme.Color.Border.SecondaryDarkActive);
        addCssVar(BitCss.Var.Color.Border.Secondary.Light, bitTheme.Color.Border.SecondaryLight);
        addCssVar(BitCss.Var.Color.Border.Secondary.Hover.Light, bitTheme.Color.Border.SecondaryLightHover);
        addCssVar(BitCss.Var.Color.Border.Secondary.Active.Light, bitTheme.Color.Border.SecondaryLightActive);
        addCssVar(BitCss.Var.Color.Border.Secondary.Disabled, bitTheme.Color.Border.SecondaryDisabled);
        addCssVar(BitCss.Var.Color.Border.Secondary.DisabledText, bitTheme.Color.Border.SecondaryDisabledText);
        addCssVar(BitCss.Var.Color.Border.Secondary.Focus, bitTheme.Color.Border.SecondaryFocus);
        addCssVar(BitCss.Var.Color.Border.Tertiary.Main, bitTheme.Color.Border.Tertiary);
        addCssVar(BitCss.Var.Color.Border.Tertiary.Hover.Main, bitTheme.Color.Border.TertiaryHover);
        addCssVar(BitCss.Var.Color.Border.Tertiary.Active.Main, bitTheme.Color.Border.TertiaryActive);
        addCssVar(BitCss.Var.Color.Border.Tertiary.Dark, bitTheme.Color.Border.TertiaryDark);
        addCssVar(BitCss.Var.Color.Border.Tertiary.Hover.Dark, bitTheme.Color.Border.TertiaryDarkHover);
        addCssVar(BitCss.Var.Color.Border.Tertiary.Active.Dark, bitTheme.Color.Border.TertiaryDarkActive);
        addCssVar(BitCss.Var.Color.Border.Tertiary.Light, bitTheme.Color.Border.TertiaryLight);
        addCssVar(BitCss.Var.Color.Border.Tertiary.Hover.Light, bitTheme.Color.Border.TertiaryLightHover);
        addCssVar(BitCss.Var.Color.Border.Tertiary.Active.Light, bitTheme.Color.Border.TertiaryLightActive);
        addCssVar(BitCss.Var.Color.Border.Tertiary.Disabled, bitTheme.Color.Border.TertiaryDisabled);
        addCssVar(BitCss.Var.Color.Border.Tertiary.DisabledText, bitTheme.Color.Border.TertiaryDisabledText);
        addCssVar(BitCss.Var.Color.Border.Tertiary.Focus, bitTheme.Color.Border.TertiaryFocus);
        addCssVar(BitCss.Var.Color.Border.Disabled, bitTheme.Color.Border.Disabled);

        addCssVar(BitCss.Var.Color.Required, bitTheme.Color.Required);

        addCssVar(BitCss.Var.Color.Neutral.White, bitTheme.Color.Neutral.White);
        addCssVar(BitCss.Var.Color.Neutral.Black, bitTheme.Color.Neutral.Black);
        addCssVar(BitCss.Var.Color.Neutral.Gray10, bitTheme.Color.Neutral.Gray10);
        addCssVar(BitCss.Var.Color.Neutral.Gray20, bitTheme.Color.Neutral.Gray20);
        addCssVar(BitCss.Var.Color.Neutral.Gray30, bitTheme.Color.Neutral.Gray30);
        addCssVar(BitCss.Var.Color.Neutral.Gray40, bitTheme.Color.Neutral.Gray40);
        addCssVar(BitCss.Var.Color.Neutral.Gray50, bitTheme.Color.Neutral.Gray50);
        addCssVar(BitCss.Var.Color.Neutral.Gray60, bitTheme.Color.Neutral.Gray60);
        addCssVar(BitCss.Var.Color.Neutral.Gray70, bitTheme.Color.Neutral.Gray70);
        addCssVar(BitCss.Var.Color.Neutral.Gray80, bitTheme.Color.Neutral.Gray80);
        addCssVar(BitCss.Var.Color.Neutral.Gray90, bitTheme.Color.Neutral.Gray90);
        addCssVar(BitCss.Var.Color.Neutral.Gray100, bitTheme.Color.Neutral.Gray100);
        addCssVar(BitCss.Var.Color.Neutral.Gray110, bitTheme.Color.Neutral.Gray110);
        addCssVar(BitCss.Var.Color.Neutral.Gray120, bitTheme.Color.Neutral.Gray120);
        addCssVar(BitCss.Var.Color.Neutral.Gray130, bitTheme.Color.Neutral.Gray130);
        addCssVar(BitCss.Var.Color.Neutral.Gray140, bitTheme.Color.Neutral.Gray140);
        addCssVar(BitCss.Var.Color.Neutral.Gray150, bitTheme.Color.Neutral.Gray150);
        addCssVar(BitCss.Var.Color.Neutral.Gray160, bitTheme.Color.Neutral.Gray160);
        addCssVar(BitCss.Var.Color.Neutral.Gray170, bitTheme.Color.Neutral.Gray170);
        addCssVar(BitCss.Var.Color.Neutral.Gray180, bitTheme.Color.Neutral.Gray180);
        addCssVar(BitCss.Var.Color.Neutral.Gray190, bitTheme.Color.Neutral.Gray190);
        addCssVar(BitCss.Var.Color.Neutral.Gray200, bitTheme.Color.Neutral.Gray200);
        addCssVar(BitCss.Var.Color.Neutral.Gray210, bitTheme.Color.Neutral.Gray210);
        addCssVar(BitCss.Var.Color.Neutral.Gray220, bitTheme.Color.Neutral.Gray220);

        addCssVar(BitCss.Var.Semantic.SurfacePage, bitTheme.Color.Semantic.SurfacePage);
        addCssVar(BitCss.Var.Semantic.SurfaceElevated, bitTheme.Color.Semantic.SurfaceElevated);
        addCssVar(BitCss.Var.Semantic.SurfaceMuted, bitTheme.Color.Semantic.SurfaceMuted);
        addCssVar(BitCss.Var.Semantic.TextPrimary, bitTheme.Color.Semantic.TextPrimary);
        addCssVar(BitCss.Var.Semantic.TextSecondary, bitTheme.Color.Semantic.TextSecondary);
        addCssVar(BitCss.Var.Semantic.BorderDefault, bitTheme.Color.Semantic.BorderDefault);
        addCssVar(BitCss.Var.Semantic.AccentPrimary, bitTheme.Color.Semantic.AccentPrimary);
        addCssVar(BitCss.Var.Semantic.FocusRing, bitTheme.Color.Semantic.FocusRing);
        addCssVar(BitCss.Var.Semantic.FocusColor, bitTheme.Color.Semantic.FocusColor);

        addCssVar(BitCss.Var.Shadow.Callout, bitTheme.BoxShadow.Callout);
        addCssVar(BitCss.Var.Shadow.Callout2, bitTheme.BoxShadow.Callout2);
        addCssVar(BitCss.Var.Shadow.Sm, bitTheme.BoxShadow.Sm);
        addCssVar(BitCss.Var.Shadow.Nm, bitTheme.BoxShadow.Nm);
        addCssVar(BitCss.Var.Shadow.Md, bitTheme.BoxShadow.Md);
        addCssVar(BitCss.Var.Shadow.Lg, bitTheme.BoxShadow.Lg);
        addCssVar(BitCss.Var.Shadow.Xl, bitTheme.BoxShadow.Xl);
        addCssVar(BitCss.Var.Shadow.Xxl, bitTheme.BoxShadow.Xxl);
        addCssVar(BitCss.Var.Shadow.Inner, bitTheme.BoxShadow.Inner);
        addCssVar(BitCss.Var.Shadow.S1, bitTheme.BoxShadow.S1);
        addCssVar(BitCss.Var.Shadow.S2, bitTheme.BoxShadow.S2);
        addCssVar(BitCss.Var.Shadow.S3, bitTheme.BoxShadow.S3);
        addCssVar(BitCss.Var.Shadow.S4, bitTheme.BoxShadow.S4);
        addCssVar(BitCss.Var.Shadow.S5, bitTheme.BoxShadow.S5);
        addCssVar(BitCss.Var.Shadow.S6, bitTheme.BoxShadow.S6);
        addCssVar(BitCss.Var.Shadow.S7, bitTheme.BoxShadow.S7);
        addCssVar(BitCss.Var.Shadow.S8, bitTheme.BoxShadow.S8);
        addCssVar(BitCss.Var.Shadow.S9, bitTheme.BoxShadow.S9);
        addCssVar(BitCss.Var.Shadow.S10, bitTheme.BoxShadow.S10);
        addCssVar(BitCss.Var.Shadow.S11, bitTheme.BoxShadow.S11);
        addCssVar(BitCss.Var.Shadow.S12, bitTheme.BoxShadow.S12);
        addCssVar(BitCss.Var.Shadow.S13, bitTheme.BoxShadow.S13);
        addCssVar(BitCss.Var.Shadow.S14, bitTheme.BoxShadow.S14);
        addCssVar(BitCss.Var.Shadow.S15, bitTheme.BoxShadow.S15);
        addCssVar(BitCss.Var.Shadow.S16, bitTheme.BoxShadow.S16);
        addCssVar(BitCss.Var.Shadow.S17, bitTheme.BoxShadow.S17);
        addCssVar(BitCss.Var.Shadow.S18, bitTheme.BoxShadow.S18);
        addCssVar(BitCss.Var.Shadow.S19, bitTheme.BoxShadow.S19);
        addCssVar(BitCss.Var.Shadow.S20, bitTheme.BoxShadow.S20);
        addCssVar(BitCss.Var.Shadow.S21, bitTheme.BoxShadow.S21);
        addCssVar(BitCss.Var.Shadow.S22, bitTheme.BoxShadow.S22);
        addCssVar(BitCss.Var.Shadow.S23, bitTheme.BoxShadow.S23);
        addCssVar(BitCss.Var.Shadow.S24, bitTheme.BoxShadow.S24);
        addCssVar(BitCss.Var.Shadow.FocusRing, bitTheme.BoxShadow.FocusRing);
        addCssVar(BitCss.Var.Shadow.Card, bitTheme.BoxShadow.Card);
        addCssVar(BitCss.Var.Shadow.Popup, bitTheme.BoxShadow.Popup);
        addCssVar(BitCss.Var.Shadow.Dialog, bitTheme.BoxShadow.Dialog);
        addCssVar(BitCss.Var.Shadow.Sheet, bitTheme.BoxShadow.Sheet);
        addCssVar(BitCss.Var.Shadow.Tooltip, bitTheme.BoxShadow.Tooltip);
        addCssVar(BitCss.Var.Shadow.Snackbar, bitTheme.BoxShadow.Snackbar);
        addCssVar(BitCss.Var.Shadow.AppBarTop, bitTheme.BoxShadow.AppBarTop);
        addCssVar(BitCss.Var.Shadow.AppBarBottom, bitTheme.BoxShadow.AppBarBottom);

        addCssVar(BitCss.Var.Spacing.ScalingFactor, bitTheme.Spacing.ScalingFactor);
        addCssVar(BitCss.Var.Spacing.Dialog, bitTheme.Spacing.Dialog);

        addCssVar(BitCss.Var.ZIndex.Snackbar, bitTheme.ZIndex.Snackbar);
        addCssVar(BitCss.Var.ZIndex.Modal, bitTheme.ZIndex.Modal);
        addCssVar(BitCss.Var.ZIndex.Callout, bitTheme.ZIndex.Callout);
        addCssVar(BitCss.Var.ZIndex.Overlay, bitTheme.ZIndex.Overlay);
        addCssVar(BitCss.Var.ZIndex.Base, bitTheme.ZIndex.Base);

        addCssVar(BitCss.Var.Shape.BorderRadius, bitTheme.Shape.BorderRadius);
        addCssVar(BitCss.Var.Shape.BorderWidth, bitTheme.Shape.BorderWidth);
        addCssVar(BitCss.Var.Shape.BorderStyle, bitTheme.Shape.BorderStyle);
        addCssVar(BitCss.Var.Shape.FocusRingWidth, bitTheme.Shape.FocusRingWidth);
        addCssVar(BitCss.Var.Shape.FocusRingOffset, bitTheme.Shape.FocusRingOffset);
        addCssVar(BitCss.Var.Shape.BorderWidthThick, bitTheme.Shape.BorderWidthThick);
        addCssVar(BitCss.Var.Shape.Radius.None, bitTheme.Shape.Radius.None);
        addCssVar(BitCss.Var.Shape.Radius.Xs, bitTheme.Shape.Radius.Xs);
        addCssVar(BitCss.Var.Shape.Radius.Sm, bitTheme.Shape.Radius.Sm);
        addCssVar(BitCss.Var.Shape.Radius.Md, bitTheme.Shape.Radius.Md);
        addCssVar(BitCss.Var.Shape.Radius.Lg, bitTheme.Shape.Radius.Lg);
        addCssVar(BitCss.Var.Shape.Radius.Xl, bitTheme.Shape.Radius.Xl);
        addCssVar(BitCss.Var.Shape.Radius.Xxl, bitTheme.Shape.Radius.Xxl);
        addCssVar(BitCss.Var.Shape.Radius.Full, bitTheme.Shape.Radius.Full);
        addCssVar(BitCss.Var.Shape.Radius.Control, bitTheme.Shape.Radius.Control);
        addCssVar(BitCss.Var.Shape.Radius.Button, bitTheme.Shape.Radius.Button);
        addCssVar(BitCss.Var.Shape.Radius.Chip, bitTheme.Shape.Radius.Chip);
        addCssVar(BitCss.Var.Shape.Radius.Selection, bitTheme.Shape.Radius.Selection);
        addCssVar(BitCss.Var.Shape.Radius.Surface, bitTheme.Shape.Radius.Surface);
        addCssVar(BitCss.Var.Shape.Radius.Popup, bitTheme.Shape.Radius.Popup);
        addCssVar(BitCss.Var.Shape.Radius.Dialog, bitTheme.Shape.Radius.Dialog);

        addCssVar(BitCss.Var.Typography.FontFamily, bitTheme.Typography.FontFamily);
        addCssVar(BitCss.Var.Typography.MonoFontFamily, bitTheme.Typography.MonoFontFamily);
        addCssVar(BitCss.Var.Typography.FontWeight, bitTheme.Typography.FontWeight);
        addCssVar(BitCss.Var.Typography.LineHeight, bitTheme.Typography.LineHeight);
        addCssVar(BitCss.Var.Typography.GutterSize, bitTheme.Typography.GutterSize);

        addCssVar(BitCss.Var.Typography.FontSize.Xxs, bitTheme.Typography.FontSize.Xxs);
        addCssVar(BitCss.Var.Typography.FontSize.Xs, bitTheme.Typography.FontSize.Xs);
        addCssVar(BitCss.Var.Typography.FontSize.Sm, bitTheme.Typography.FontSize.Sm);
        addCssVar(BitCss.Var.Typography.FontSize.Md, bitTheme.Typography.FontSize.Md);
        addCssVar(BitCss.Var.Typography.FontSize.Lg, bitTheme.Typography.FontSize.Lg);
        addCssVar(BitCss.Var.Typography.FontSize.Xl, bitTheme.Typography.FontSize.Xl);
        addCssVar(BitCss.Var.Typography.FontSize.Xxl, bitTheme.Typography.FontSize.Xxl);
        addCssVar(BitCss.Var.Typography.FontSize.Xxxl, bitTheme.Typography.FontSize.Xxxl);
        addCssVar(BitCss.Var.Typography.FontSize.Xxxxl, bitTheme.Typography.FontSize.Xxxxl);

        addCssVar(BitCss.Var.Typography.FontWeights.Light, bitTheme.Typography.FontWeights.Light);
        addCssVar(BitCss.Var.Typography.FontWeights.Regular, bitTheme.Typography.FontWeights.Regular);
        addCssVar(BitCss.Var.Typography.FontWeights.Medium, bitTheme.Typography.FontWeights.Medium);
        addCssVar(BitCss.Var.Typography.FontWeights.SemiBold, bitTheme.Typography.FontWeights.SemiBold);
        addCssVar(BitCss.Var.Typography.FontWeights.Bold, bitTheme.Typography.FontWeights.Bold);

        addCssVar(BitCss.Var.Typography.Control.LetterSpacing, bitTheme.Typography.Control.LetterSpacing);
        addCssVar(BitCss.Var.Typography.Control.TextTransform, bitTheme.Typography.Control.TextTransform);

        addCssVar(BitCss.Var.Typography.Body1.Margin, bitTheme.Typography.Body1.Margin);
        addCssVar(BitCss.Var.Typography.Body1.FontWeight, bitTheme.Typography.Body1.FontWeight);
        addCssVar(BitCss.Var.Typography.Body1.FontSize, bitTheme.Typography.Body1.FontSize);
        addCssVar(BitCss.Var.Typography.Body1.LineHeight, bitTheme.Typography.Body1.LineHeight);
        addCssVar(BitCss.Var.Typography.Body1.LetterSpacing, bitTheme.Typography.Body1.LetterSpacing);

        addCssVar(BitCss.Var.Typography.Body2.Margin, bitTheme.Typography.Body2.Margin);
        addCssVar(BitCss.Var.Typography.Body2.FontWeight, bitTheme.Typography.Body2.FontWeight);
        addCssVar(BitCss.Var.Typography.Body2.FontSize, bitTheme.Typography.Body2.FontSize);
        addCssVar(BitCss.Var.Typography.Body2.LineHeight, bitTheme.Typography.Body2.LineHeight);
        addCssVar(BitCss.Var.Typography.Body2.LetterSpacing, bitTheme.Typography.Body2.LetterSpacing);

        addCssVar(BitCss.Var.Typography.Button.Margin, bitTheme.Typography.Button.Margin);
        addCssVar(BitCss.Var.Typography.Button.FontWeight, bitTheme.Typography.Button.FontWeight);
        addCssVar(BitCss.Var.Typography.Button.FontSize, bitTheme.Typography.Button.FontSize);
        addCssVar(BitCss.Var.Typography.Button.LineHeight, bitTheme.Typography.Button.LineHeight);
        addCssVar(BitCss.Var.Typography.Button.LetterSpacing, bitTheme.Typography.Button.LetterSpacing);
        addCssVar(BitCss.Var.Typography.Button.TextTransform, bitTheme.Typography.Button.TextTransform);
        addCssVar(BitCss.Var.Typography.Button.Display, bitTheme.Typography.Button.Display);

        addCssVar(BitCss.Var.Typography.Caption1.Margin, bitTheme.Typography.Caption1.Margin);
        addCssVar(BitCss.Var.Typography.Caption1.FontWeight, bitTheme.Typography.Caption1.FontWeight);
        addCssVar(BitCss.Var.Typography.Caption1.FontSize, bitTheme.Typography.Caption1.FontSize);
        addCssVar(BitCss.Var.Typography.Caption1.LineHeight, bitTheme.Typography.Caption1.LineHeight);
        addCssVar(BitCss.Var.Typography.Caption1.LetterSpacing, bitTheme.Typography.Caption1.LetterSpacing);

        addCssVar(BitCss.Var.Typography.Caption2.Margin, bitTheme.Typography.Caption2.Margin);
        addCssVar(BitCss.Var.Typography.Caption2.FontWeight, bitTheme.Typography.Caption2.FontWeight);
        addCssVar(BitCss.Var.Typography.Caption2.FontSize, bitTheme.Typography.Caption2.FontSize);
        addCssVar(BitCss.Var.Typography.Caption2.LineHeight, bitTheme.Typography.Caption2.LineHeight);
        addCssVar(BitCss.Var.Typography.Caption2.LetterSpacing, bitTheme.Typography.Caption2.LetterSpacing);

        addCssVar(BitCss.Var.Typography.H1.Margin, bitTheme.Typography.H1.Margin);
        addCssVar(BitCss.Var.Typography.H1.FontWeight, bitTheme.Typography.H1.FontWeight);
        addCssVar(BitCss.Var.Typography.H1.FontSize, bitTheme.Typography.H1.FontSize);
        addCssVar(BitCss.Var.Typography.H1.LineHeight, bitTheme.Typography.H1.LineHeight);
        addCssVar(BitCss.Var.Typography.H1.LetterSpacing, bitTheme.Typography.H1.LetterSpacing);

        addCssVar(BitCss.Var.Typography.H2.Margin, bitTheme.Typography.H2.Margin);
        addCssVar(BitCss.Var.Typography.H2.FontWeight, bitTheme.Typography.H2.FontWeight);
        addCssVar(BitCss.Var.Typography.H2.FontSize, bitTheme.Typography.H2.FontSize);
        addCssVar(BitCss.Var.Typography.H2.LineHeight, bitTheme.Typography.H2.LineHeight);
        addCssVar(BitCss.Var.Typography.H2.LetterSpacing, bitTheme.Typography.H2.LetterSpacing);

        addCssVar(BitCss.Var.Typography.H3.Margin, bitTheme.Typography.H3.Margin);
        addCssVar(BitCss.Var.Typography.H3.FontWeight, bitTheme.Typography.H3.FontWeight);
        addCssVar(BitCss.Var.Typography.H3.FontSize, bitTheme.Typography.H3.FontSize);
        addCssVar(BitCss.Var.Typography.H3.LineHeight, bitTheme.Typography.H3.LineHeight);
        addCssVar(BitCss.Var.Typography.H3.LetterSpacing, bitTheme.Typography.H3.LetterSpacing);

        addCssVar(BitCss.Var.Typography.H4.Margin, bitTheme.Typography.H4.Margin);
        addCssVar(BitCss.Var.Typography.H4.FontWeight, bitTheme.Typography.H4.FontWeight);
        addCssVar(BitCss.Var.Typography.H4.FontSize, bitTheme.Typography.H4.FontSize);
        addCssVar(BitCss.Var.Typography.H4.LineHeight, bitTheme.Typography.H4.LineHeight);
        addCssVar(BitCss.Var.Typography.H4.LetterSpacing, bitTheme.Typography.H4.LetterSpacing);

        addCssVar(BitCss.Var.Typography.H5.Margin, bitTheme.Typography.H5.Margin);
        addCssVar(BitCss.Var.Typography.H5.FontWeight, bitTheme.Typography.H5.FontWeight);
        addCssVar(BitCss.Var.Typography.H5.FontSize, bitTheme.Typography.H5.FontSize);
        addCssVar(BitCss.Var.Typography.H5.LineHeight, bitTheme.Typography.H5.LineHeight);
        addCssVar(BitCss.Var.Typography.H5.LetterSpacing, bitTheme.Typography.H5.LetterSpacing);

        addCssVar(BitCss.Var.Typography.H6.Margin, bitTheme.Typography.H6.Margin);
        addCssVar(BitCss.Var.Typography.H6.FontWeight, bitTheme.Typography.H6.FontWeight);
        addCssVar(BitCss.Var.Typography.H6.FontSize, bitTheme.Typography.H6.FontSize);
        addCssVar(BitCss.Var.Typography.H6.LineHeight, bitTheme.Typography.H6.LineHeight);
        addCssVar(BitCss.Var.Typography.H6.LetterSpacing, bitTheme.Typography.H6.LetterSpacing);

        addCssVar(BitCss.Var.Typography.Inherit.Margin, bitTheme.Typography.Inherit.Margin);
        addCssVar(BitCss.Var.Typography.Inherit.FontFamily, bitTheme.Typography.Inherit.FontFamily);
        addCssVar(BitCss.Var.Typography.Inherit.FontWeight, bitTheme.Typography.Inherit.FontWeight);
        addCssVar(BitCss.Var.Typography.Inherit.FontSize, bitTheme.Typography.Inherit.FontSize);
        addCssVar(BitCss.Var.Typography.Inherit.LineHeight, bitTheme.Typography.Inherit.LineHeight);
        addCssVar(BitCss.Var.Typography.Inherit.LetterSpacing, bitTheme.Typography.Inherit.LetterSpacing);
        addCssVar(BitCss.Var.Typography.Inherit.TextTransform, bitTheme.Typography.Inherit.TextTransform);
        addCssVar(BitCss.Var.Typography.Inherit.Display, bitTheme.Typography.Inherit.Display);

        addCssVar(BitCss.Var.Typography.Overline.Margin, bitTheme.Typography.Overline.Margin);
        addCssVar(BitCss.Var.Typography.Overline.FontWeight, bitTheme.Typography.Overline.FontWeight);
        addCssVar(BitCss.Var.Typography.Overline.FontSize, bitTheme.Typography.Overline.FontSize);
        addCssVar(BitCss.Var.Typography.Overline.LineHeight, bitTheme.Typography.Overline.LineHeight);
        addCssVar(BitCss.Var.Typography.Overline.LetterSpacing, bitTheme.Typography.Overline.LetterSpacing);
        addCssVar(BitCss.Var.Typography.Overline.TextTransform, bitTheme.Typography.Overline.TextTransform);
        addCssVar(BitCss.Var.Typography.Overline.Display, bitTheme.Typography.Overline.Display);

        addCssVar(BitCss.Var.Typography.Subtitle1.Margin, bitTheme.Typography.Subtitle1.Margin);
        addCssVar(BitCss.Var.Typography.Subtitle1.FontWeight, bitTheme.Typography.Subtitle1.FontWeight);
        addCssVar(BitCss.Var.Typography.Subtitle1.FontSize, bitTheme.Typography.Subtitle1.FontSize);
        addCssVar(BitCss.Var.Typography.Subtitle1.LineHeight, bitTheme.Typography.Subtitle1.LineHeight);
        addCssVar(BitCss.Var.Typography.Subtitle1.LetterSpacing, bitTheme.Typography.Subtitle1.LetterSpacing);

        addCssVar(BitCss.Var.Typography.Subtitle2.Margin, bitTheme.Typography.Subtitle2.Margin);
        addCssVar(BitCss.Var.Typography.Subtitle2.FontWeight, bitTheme.Typography.Subtitle2.FontWeight);
        addCssVar(BitCss.Var.Typography.Subtitle2.FontSize, bitTheme.Typography.Subtitle2.FontSize);
        addCssVar(BitCss.Var.Typography.Subtitle2.LineHeight, bitTheme.Typography.Subtitle2.LineHeight);
        addCssVar(BitCss.Var.Typography.Subtitle2.LetterSpacing, bitTheme.Typography.Subtitle2.LetterSpacing);

        addCssVar(BitCss.Var.Motion.Duration, bitTheme.Motion.Duration);
        addCssVar(BitCss.Var.Motion.DurationShort, bitTheme.Motion.DurationShort);
        addCssVar(BitCss.Var.Motion.DurationLong, bitTheme.Motion.DurationLong);
        addCssVar(BitCss.Var.Motion.Easing, bitTheme.Motion.EasingStandard);
        addCssVar(BitCss.Var.Motion.EasingDecelerate, bitTheme.Motion.EasingDecelerate);
        addCssVar(BitCss.Var.Motion.EasingAccelerate, bitTheme.Motion.EasingAccelerate);
        addCssVar(BitCss.Var.Motion.DurationSpinner, bitTheme.Motion.DurationSpinner);
        addCssVar(BitCss.Var.Motion.EasingSpinner, bitTheme.Motion.EasingSpinner);
        addCssVar(BitCss.Var.Motion.LoopFactor, bitTheme.Motion.LoopFactor);

        addCssVar(BitCss.Var.Layout.DensityScale, bitTheme.Layout.DensityScale);
        addCssVar(BitCss.Var.Layout.DialogActionsDirection, bitTheme.Layout.DialogActionsDirection);
        addCssVar(BitCss.Var.Layout.DialogActionsJustify, bitTheme.Layout.DialogActionsJustify);
        addCssVar(BitCss.Var.Layout.DialogActionsAlign, bitTheme.Layout.DialogActionsAlign);

        addCssVar(BitCss.Var.Layout.Breakpoints.Xs, bitTheme.Layout.Breakpoints.Xs);
        addCssVar(BitCss.Var.Layout.Breakpoints.Sm, bitTheme.Layout.Breakpoints.Sm);
        addCssVar(BitCss.Var.Layout.Breakpoints.Md, bitTheme.Layout.Breakpoints.Md);
        addCssVar(BitCss.Var.Layout.Breakpoints.Lg, bitTheme.Layout.Breakpoints.Lg);
        addCssVar(BitCss.Var.Layout.Breakpoints.Xl, bitTheme.Layout.Breakpoints.Xl);
        addCssVar(BitCss.Var.Layout.Breakpoints.Xxl, bitTheme.Layout.Breakpoints.Xxl);

        addCssVar(BitCss.Var.Size.Control.Sm, bitTheme.Size.Control.Sm);
        addCssVar(BitCss.Var.Size.Control.Md, bitTheme.Size.Control.Md);
        addCssVar(BitCss.Var.Size.Control.Lg, bitTheme.Size.Control.Lg);
        addCssVar(BitCss.Var.Size.ControlPaddingX.Sm, bitTheme.Size.ControlPaddingX.Sm);
        addCssVar(BitCss.Var.Size.ControlPaddingX.Md, bitTheme.Size.ControlPaddingX.Md);
        addCssVar(BitCss.Var.Size.ControlPaddingX.Lg, bitTheme.Size.ControlPaddingX.Lg);
        addCssVar(BitCss.Var.Size.ControlPaddingY.Sm, bitTheme.Size.ControlPaddingY.Sm);
        addCssVar(BitCss.Var.Size.ControlPaddingY.Md, bitTheme.Size.ControlPaddingY.Md);
        addCssVar(BitCss.Var.Size.ControlPaddingY.Lg, bitTheme.Size.ControlPaddingY.Lg);
        addCssVar(BitCss.Var.Size.ControlMinWidth, bitTheme.Size.ControlMinWidth);
        addCssVar(BitCss.Var.Size.Icon.Sm, bitTheme.Size.Icon.Sm);
        addCssVar(BitCss.Var.Size.Icon.Md, bitTheme.Size.Icon.Md);
        addCssVar(BitCss.Var.Size.Icon.Lg, bitTheme.Size.Icon.Lg);
        addCssVar(BitCss.Var.Size.Selection.Sm, bitTheme.Size.Selection.Sm);
        addCssVar(BitCss.Var.Size.Selection.Md, bitTheme.Size.Selection.Md);
        addCssVar(BitCss.Var.Size.Selection.Lg, bitTheme.Size.Selection.Lg);
        addCssVar(BitCss.Var.Size.Item.Sm, bitTheme.Size.Item.Sm);
        addCssVar(BitCss.Var.Size.Item.Md, bitTheme.Size.Item.Md);
        addCssVar(BitCss.Var.Size.Item.Lg, bitTheme.Size.Item.Lg);
        addCssVar(BitCss.Var.Size.Tab, bitTheme.Size.Tab);
        addCssVar(BitCss.Var.Size.TabIndicator, bitTheme.Size.TabIndicator);
        addCssVar(BitCss.Var.Size.Divider, bitTheme.Size.Divider);
        addCssVar(BitCss.Var.Size.Track.Sm, bitTheme.Size.Track.Sm);
        addCssVar(BitCss.Var.Size.Track.Md, bitTheme.Size.Track.Md);
        addCssVar(BitCss.Var.Size.Track.Lg, bitTheme.Size.Track.Lg);
        addCssVar(BitCss.Var.Size.SwitchWidth.Sm, bitTheme.Size.Switch.Width.Sm);
        addCssVar(BitCss.Var.Size.SwitchWidth.Md, bitTheme.Size.Switch.Width.Md);
        addCssVar(BitCss.Var.Size.SwitchWidth.Lg, bitTheme.Size.Switch.Width.Lg);
        addCssVar(BitCss.Var.Size.SwitchHeight.Sm, bitTheme.Size.Switch.Height.Sm);
        addCssVar(BitCss.Var.Size.SwitchHeight.Md, bitTheme.Size.Switch.Height.Md);
        addCssVar(BitCss.Var.Size.SwitchHeight.Lg, bitTheme.Size.Switch.Height.Lg);
        addCssVar(BitCss.Var.Size.SwitchThumb.Sm, bitTheme.Size.Switch.Thumb.Sm);
        addCssVar(BitCss.Var.Size.SwitchThumb.Md, bitTheme.Size.Switch.Thumb.Md);
        addCssVar(BitCss.Var.Size.SwitchThumb.Lg, bitTheme.Size.Switch.Thumb.Lg);
        addCssVar(BitCss.Var.Size.SliderThumb.Sm, bitTheme.Size.SliderThumb.Sm);
        addCssVar(BitCss.Var.Size.SliderThumb.Md, bitTheme.Size.SliderThumb.Md);
        addCssVar(BitCss.Var.Size.SliderThumb.Lg, bitTheme.Size.SliderThumb.Lg);
        addCssVar(BitCss.Var.Size.SpinnerStroke, bitTheme.Size.SpinnerStroke);
        addCssVar(BitCss.Var.Size.PopupMaxHeight, bitTheme.Size.PopupMaxHeight);
        addCssVar(BitCss.Var.Size.DialogMaxWidth, bitTheme.Size.DialogMaxWidth);

        addCssVar(BitCss.Var.Opacity.Disabled, bitTheme.Opacity.Disabled);

        return result;

        // Skip null *and* empty/whitespace-only values so we never emit an invalid declaration
        // like `--bit-x: ;` (which the browser drops anyway, but only after parsing). Treating
        // whitespace as "skip" matches the convention used elsewhere in the theme system, e.g.
        // BitThemeColorDerivation's IsNullOrWhiteSpace guard.
        void addCssVar(string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            // Drop values that could break out of a single CSS declaration. Token values can come
            // from untrusted sources (BitThemeSerialization is documented for storage / admin UIs /
            // sharing brand tokens) and are emitted both into an inline `style` attribute
            // (BitThemeProvider) and via element.style.setProperty (BitTheme.ts). Skipping mirrors
            // the whitespace guard above: the element falls back to the stylesheet default.
            if (IsUnsafeCssTokenValue(value!)) return;
            result!.Add(key, value!);
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> when a CSS custom-property value contains characters that
    /// could escape a single declaration and inject additional CSS.
    /// </summary>
    /// <remarks>
    /// The primary sink is the inline <c>style</c> attribute produced by <see cref="BitThemeProvider"/>,
    /// where the declarations are concatenated as <c>--name:value;--name:value</c>. A value
    /// containing <c>;</c> would start a new declaration, <c>/*</c> would comment out the rest of the
    /// inline style, and <c>{</c>/<c>}</c>/<c>&lt;</c>/<c>&gt;</c> are never valid inside a single
    /// property value. These characters are not needed by any legitimate theme token (colors,
    /// shadows, sizes, durations, easings, font-family lists - none of which use them), so rejecting
    /// the whole value is safe and avoids partially-stripped, malformed output.
    /// </remarks>
    // Internal (not private) so BitThemeUtilities.WithAlpha can validate its input against the
    // exact same rule the emission path enforces, instead of duplicating the character list.
    internal static bool IsUnsafeCssTokenValue(string value)
    {
        var doubleQuotes = 0;
        var singleQuotes = 0;

        foreach (var ch in value)
        {
            switch (ch)
            {
                case ';':   // ends the declaration in the inline-style concatenation
                case '{':
                case '}':   // CSS block delimiters
                case '<':
                case '>':   // HTML metacharacters / </style> breakout defense-in-depth
                case '\\':  // CSS escape sequences
                case '\0':
                case '\n':
                case '\r':
                case '\f':  // null / newlines / form feed
                    return true;
                case '"':
                    doubleQuotes++;
                    break;
                case '\'':
                    singleQuotes++;
                    break;
            }
        }

        // An unbalanced (odd count) quote leaves a CSS string open at the end of the declaration,
        // so the browser's tokenizer swallows the following ";--next:…" declarations (and the user's
        // trailing style) up to the next quote or EOF - silently dropping sibling tokens to their
        // stylesheet defaults. Balanced quotes are legitimate (e.g. a font-family value like
        // '"Segoe UI", Arial'), so only reject an unmatched count rather than quotes wholesale.
        // Backslash is already rejected above, so there are no escaped quotes to account for here.
        if ((doubleQuotes & 1) == 1 || (singleQuotes & 1) == 1) return true;

        // Comment markers could swallow trailing declarations in the inline-style concatenation
        // (e.g. "red/*" eating the following ";--next:..." up to a later "*/").
        return value.Contains("/*", StringComparison.Ordinal)
            || value.Contains("*/", StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns a copy of <paramref name="src"/> with every branch object non-null, WITHOUT mutating
    /// the caller's instance. Used by <see cref="MapToCssVariables"/> and <see cref="Merge"/> so a
    /// hand-constructed sparse theme (null branches, reachable via the public setters) can be walked
    /// without a <see cref="NullReferenceException"/>.
    /// </summary>
    /// <remarks>
    /// Only the container types that hold <em>sub-objects</em> (<see cref="BitTheme"/>,
    /// <see cref="BitThemeColors"/>, <see cref="BitThemeTypography"/>, <see cref="BitThemeLayout"/>)
    /// are freshly allocated. Leaf-holding branch objects (the color/typography/etc. variants, whose
    /// properties are all immutable <see cref="string"/>s) are shared by reference: callers only ever
    /// read from the normalized graph here - the merge writes into a separate fresh result - so no
    /// shared object is mutated and no per-leaf copy is needed. Direct string leaves on the container
    /// types (e.g. <see cref="BitThemeColors.Required"/>, <see cref="BitThemeTypography.FontFamily"/>,
    /// <see cref="BitThemeLayout.DensityScale"/>) are carried across explicitly.
    /// </remarks>
    private static BitTheme NormalizeToNew(BitTheme? src)
    {
        src ??= new BitTheme();

        return new BitTheme
        {
            Color = NormalizeColors(src.Color),
            BoxShadow = src.BoxShadow ?? new(),
            Spacing = src.Spacing ?? new(),
            ZIndex = src.ZIndex ?? new(),
            Shape = NormalizeShape(src.Shape),
            Typography = NormalizeTypography(src.Typography),
            Motion = src.Motion ?? new(),
            Layout = NormalizeLayout(src.Layout),
            Size = NormalizeSize(src.Size),
            Opacity = src.Opacity ?? new(),
        };
    }

    private static BitThemeColors NormalizeColors(BitThemeColors? src)
    {
        src ??= new BitThemeColors();

        return new BitThemeColors
        {
            Primary = src.Primary ?? new(),
            Secondary = src.Secondary ?? new(),
            Tertiary = src.Tertiary ?? new(),
            Info = src.Info ?? new(),
            Success = src.Success ?? new(),
            Warning = src.Warning ?? new(),
            SevereWarning = src.SevereWarning ?? new(),
            Error = src.Error ?? new(),
            Foreground = src.Foreground ?? new(),
            Background = src.Background ?? new(),
            Border = src.Border ?? new(),
            Neutral = src.Neutral ?? new(),
            Semantic = src.Semantic ?? new(),
            Required = src.Required,
        };
    }

    private static BitThemeTypography NormalizeTypography(BitThemeTypography? src)
    {
        src ??= new BitThemeTypography();

        return new BitThemeTypography
        {
            FontFamily = src.FontFamily,
            MonoFontFamily = src.MonoFontFamily,
            FontWeight = src.FontWeight,
            LineHeight = src.LineHeight,
            GutterSize = src.GutterSize,
            FontSize = src.FontSize ?? new(),
            FontWeights = src.FontWeights ?? new(),
            Control = src.Control ?? new(),
            H1 = src.H1 ?? new(),
            H2 = src.H2 ?? new(),
            H3 = src.H3 ?? new(),
            H4 = src.H4 ?? new(),
            H5 = src.H5 ?? new(),
            H6 = src.H6 ?? new(),
            Subtitle1 = src.Subtitle1 ?? new(),
            Subtitle2 = src.Subtitle2 ?? new(),
            Body1 = src.Body1 ?? new(),
            Body2 = src.Body2 ?? new(),
            Button = src.Button ?? new(),
            Caption1 = src.Caption1 ?? new(),
            Caption2 = src.Caption2 ?? new(),
            Overline = src.Overline ?? new(),
            Inherit = src.Inherit ?? new(),
        };
    }

    private static BitThemeShapes NormalizeShape(BitThemeShapes? src)
    {
        src ??= new BitThemeShapes();

        return new BitThemeShapes
        {
            BorderRadius = src.BorderRadius,
            BorderWidth = src.BorderWidth,
            BorderStyle = src.BorderStyle,
            BorderWidthThick = src.BorderWidthThick,
            FocusRingWidth = src.FocusRingWidth,
            FocusRingOffset = src.FocusRingOffset,
            Radius = src.Radius ?? new(),
        };
    }

    private static BitThemeSizes NormalizeSize(BitThemeSizes? src)
    {
        src ??= new BitThemeSizes();

        return new BitThemeSizes
        {
            Control = src.Control ?? new(),
            ControlPaddingX = src.ControlPaddingX ?? new(),
            ControlPaddingY = src.ControlPaddingY ?? new(),
            ControlMinWidth = src.ControlMinWidth,
            Icon = src.Icon ?? new(),
            Selection = src.Selection ?? new(),
            Item = src.Item ?? new(),
            Tab = src.Tab,
            TabIndicator = src.TabIndicator,
            Divider = src.Divider,
            Track = src.Track ?? new(),
            Switch = NormalizeSwitchSize(src.Switch),
            SliderThumb = src.SliderThumb ?? new(),
            SpinnerStroke = src.SpinnerStroke,
            PopupMaxHeight = src.PopupMaxHeight,
            DialogMaxWidth = src.DialogMaxWidth,
        };
    }

    private static BitThemeSwitchSizes NormalizeSwitchSize(BitThemeSwitchSizes? src)
    {
        src ??= new BitThemeSwitchSizes();

        return new BitThemeSwitchSizes
        {
            Width = src.Width ?? new(),
            Height = src.Height ?? new(),
            Thumb = src.Thumb ?? new(),
        };
    }

    private static BitThemeLayout NormalizeLayout(BitThemeLayout? src)
    {
        src ??= new BitThemeLayout();

        return new BitThemeLayout
        {
            DensityScale = src.DensityScale,
            DialogActionsDirection = src.DialogActionsDirection,
            DialogActionsJustify = src.DialogActionsJustify,
            DialogActionsAlign = src.DialogActionsAlign,
            Breakpoints = src.Breakpoints ?? new(),
        };
    }

    internal static BitTheme Merge(BitTheme bitTheme, BitTheme other)
    {
        var result = new BitTheme();

        // Walk normalized COPIES so hand-constructed sparse themes (e.g. new BitTheme { Color = null }
        // or new BitThemeColors { Primary = null }, both reachable via the public setters) can be
        // traversed without NRE - WITHOUT mutating the caller's instances. (Previously this filled
        // null branch objects in place on both passed themes as a side effect.)
        bitTheme = NormalizeToNew(bitTheme);
        other = NormalizeToNew(other);

        result.Color.Primary.Main = bitTheme.Color.Primary.Main ?? other.Color.Primary.Main;
        result.Color.Primary.MainHover = bitTheme.Color.Primary.MainHover ?? other.Color.Primary.MainHover;
        result.Color.Primary.MainActive = bitTheme.Color.Primary.MainActive ?? other.Color.Primary.MainActive;
        result.Color.Primary.Dark = bitTheme.Color.Primary.Dark ?? other.Color.Primary.Dark;
        result.Color.Primary.DarkHover = bitTheme.Color.Primary.DarkHover ?? other.Color.Primary.DarkHover;
        result.Color.Primary.DarkActive = bitTheme.Color.Primary.DarkActive ?? other.Color.Primary.DarkActive;
        result.Color.Primary.Light = bitTheme.Color.Primary.Light ?? other.Color.Primary.Light;
        result.Color.Primary.LightHover = bitTheme.Color.Primary.LightHover ?? other.Color.Primary.LightHover;
        result.Color.Primary.LightActive = bitTheme.Color.Primary.LightActive ?? other.Color.Primary.LightActive;
        result.Color.Primary.Text = bitTheme.Color.Primary.Text ?? other.Color.Primary.Text;
        result.Color.Primary.Disabled = bitTheme.Color.Primary.Disabled ?? other.Color.Primary.Disabled;
        result.Color.Primary.DisabledText = bitTheme.Color.Primary.DisabledText ?? other.Color.Primary.DisabledText;
        result.Color.Primary.Focus = bitTheme.Color.Primary.Focus ?? other.Color.Primary.Focus;

        result.Color.Secondary.Main = bitTheme.Color.Secondary.Main ?? other.Color.Secondary.Main;
        result.Color.Secondary.MainHover = bitTheme.Color.Secondary.MainHover ?? other.Color.Secondary.MainHover;
        result.Color.Secondary.MainActive = bitTheme.Color.Secondary.MainActive ?? other.Color.Secondary.MainActive;
        result.Color.Secondary.Dark = bitTheme.Color.Secondary.Dark ?? other.Color.Secondary.Dark;
        result.Color.Secondary.DarkHover = bitTheme.Color.Secondary.DarkHover ?? other.Color.Secondary.DarkHover;
        result.Color.Secondary.DarkActive = bitTheme.Color.Secondary.DarkActive ?? other.Color.Secondary.DarkActive;
        result.Color.Secondary.Light = bitTheme.Color.Secondary.Light ?? other.Color.Secondary.Light;
        result.Color.Secondary.LightHover = bitTheme.Color.Secondary.LightHover ?? other.Color.Secondary.LightHover;
        result.Color.Secondary.LightActive = bitTheme.Color.Secondary.LightActive ?? other.Color.Secondary.LightActive;
        result.Color.Secondary.Text = bitTheme.Color.Secondary.Text ?? other.Color.Secondary.Text;
        result.Color.Secondary.Disabled = bitTheme.Color.Secondary.Disabled ?? other.Color.Secondary.Disabled;
        result.Color.Secondary.DisabledText = bitTheme.Color.Secondary.DisabledText ?? other.Color.Secondary.DisabledText;
        result.Color.Secondary.Focus = bitTheme.Color.Secondary.Focus ?? other.Color.Secondary.Focus;

        result.Color.Tertiary.Main = bitTheme.Color.Tertiary.Main ?? other.Color.Tertiary.Main;
        result.Color.Tertiary.MainHover = bitTheme.Color.Tertiary.MainHover ?? other.Color.Tertiary.MainHover;
        result.Color.Tertiary.MainActive = bitTheme.Color.Tertiary.MainActive ?? other.Color.Tertiary.MainActive;
        result.Color.Tertiary.Dark = bitTheme.Color.Tertiary.Dark ?? other.Color.Tertiary.Dark;
        result.Color.Tertiary.DarkHover = bitTheme.Color.Tertiary.DarkHover ?? other.Color.Tertiary.DarkHover;
        result.Color.Tertiary.DarkActive = bitTheme.Color.Tertiary.DarkActive ?? other.Color.Tertiary.DarkActive;
        result.Color.Tertiary.Light = bitTheme.Color.Tertiary.Light ?? other.Color.Tertiary.Light;
        result.Color.Tertiary.LightHover = bitTheme.Color.Tertiary.LightHover ?? other.Color.Tertiary.LightHover;
        result.Color.Tertiary.LightActive = bitTheme.Color.Tertiary.LightActive ?? other.Color.Tertiary.LightActive;
        result.Color.Tertiary.Text = bitTheme.Color.Tertiary.Text ?? other.Color.Tertiary.Text;
        result.Color.Tertiary.Disabled = bitTheme.Color.Tertiary.Disabled ?? other.Color.Tertiary.Disabled;
        result.Color.Tertiary.DisabledText = bitTheme.Color.Tertiary.DisabledText ?? other.Color.Tertiary.DisabledText;
        result.Color.Tertiary.Focus = bitTheme.Color.Tertiary.Focus ?? other.Color.Tertiary.Focus;

        result.Color.Info.Main = bitTheme.Color.Info.Main ?? other.Color.Info.Main;
        result.Color.Info.MainHover = bitTheme.Color.Info.MainHover ?? other.Color.Info.MainHover;
        result.Color.Info.MainActive = bitTheme.Color.Info.MainActive ?? other.Color.Info.MainActive;
        result.Color.Info.Dark = bitTheme.Color.Info.Dark ?? other.Color.Info.Dark;
        result.Color.Info.DarkHover = bitTheme.Color.Info.DarkHover ?? other.Color.Info.DarkHover;
        result.Color.Info.DarkActive = bitTheme.Color.Info.DarkActive ?? other.Color.Info.DarkActive;
        result.Color.Info.Light = bitTheme.Color.Info.Light ?? other.Color.Info.Light;
        result.Color.Info.LightHover = bitTheme.Color.Info.LightHover ?? other.Color.Info.LightHover;
        result.Color.Info.LightActive = bitTheme.Color.Info.LightActive ?? other.Color.Info.LightActive;
        result.Color.Info.Text = bitTheme.Color.Info.Text ?? other.Color.Info.Text;
        result.Color.Info.Disabled = bitTheme.Color.Info.Disabled ?? other.Color.Info.Disabled;
        result.Color.Info.DisabledText = bitTheme.Color.Info.DisabledText ?? other.Color.Info.DisabledText;
        result.Color.Info.Focus = bitTheme.Color.Info.Focus ?? other.Color.Info.Focus;

        result.Color.Success.Main = bitTheme.Color.Success.Main ?? other.Color.Success.Main;
        result.Color.Success.MainHover = bitTheme.Color.Success.MainHover ?? other.Color.Success.MainHover;
        result.Color.Success.MainActive = bitTheme.Color.Success.MainActive ?? other.Color.Success.MainActive;
        result.Color.Success.Dark = bitTheme.Color.Success.Dark ?? other.Color.Success.Dark;
        result.Color.Success.DarkHover = bitTheme.Color.Success.DarkHover ?? other.Color.Success.DarkHover;
        result.Color.Success.DarkActive = bitTheme.Color.Success.DarkActive ?? other.Color.Success.DarkActive;
        result.Color.Success.Light = bitTheme.Color.Success.Light ?? other.Color.Success.Light;
        result.Color.Success.LightHover = bitTheme.Color.Success.LightHover ?? other.Color.Success.LightHover;
        result.Color.Success.LightActive = bitTheme.Color.Success.LightActive ?? other.Color.Success.LightActive;
        result.Color.Success.Text = bitTheme.Color.Success.Text ?? other.Color.Success.Text;
        result.Color.Success.Disabled = bitTheme.Color.Success.Disabled ?? other.Color.Success.Disabled;
        result.Color.Success.DisabledText = bitTheme.Color.Success.DisabledText ?? other.Color.Success.DisabledText;
        result.Color.Success.Focus = bitTheme.Color.Success.Focus ?? other.Color.Success.Focus;

        result.Color.Warning.Main = bitTheme.Color.Warning.Main ?? other.Color.Warning.Main;
        result.Color.Warning.MainHover = bitTheme.Color.Warning.MainHover ?? other.Color.Warning.MainHover;
        result.Color.Warning.MainActive = bitTheme.Color.Warning.MainActive ?? other.Color.Warning.MainActive;
        result.Color.Warning.Dark = bitTheme.Color.Warning.Dark ?? other.Color.Warning.Dark;
        result.Color.Warning.DarkHover = bitTheme.Color.Warning.DarkHover ?? other.Color.Warning.DarkHover;
        result.Color.Warning.DarkActive = bitTheme.Color.Warning.DarkActive ?? other.Color.Warning.DarkActive;
        result.Color.Warning.Light = bitTheme.Color.Warning.Light ?? other.Color.Warning.Light;
        result.Color.Warning.LightHover = bitTheme.Color.Warning.LightHover ?? other.Color.Warning.LightHover;
        result.Color.Warning.LightActive = bitTheme.Color.Warning.LightActive ?? other.Color.Warning.LightActive;
        result.Color.Warning.Text = bitTheme.Color.Warning.Text ?? other.Color.Warning.Text;
        result.Color.Warning.Disabled = bitTheme.Color.Warning.Disabled ?? other.Color.Warning.Disabled;
        result.Color.Warning.DisabledText = bitTheme.Color.Warning.DisabledText ?? other.Color.Warning.DisabledText;
        result.Color.Warning.Focus = bitTheme.Color.Warning.Focus ?? other.Color.Warning.Focus;

        result.Color.SevereWarning.Main = bitTheme.Color.SevereWarning.Main ?? other.Color.SevereWarning.Main;
        result.Color.SevereWarning.MainHover = bitTheme.Color.SevereWarning.MainHover ?? other.Color.SevereWarning.MainHover;
        result.Color.SevereWarning.MainActive = bitTheme.Color.SevereWarning.MainActive ?? other.Color.SevereWarning.MainActive;
        result.Color.SevereWarning.Dark = bitTheme.Color.SevereWarning.Dark ?? other.Color.SevereWarning.Dark;
        result.Color.SevereWarning.DarkHover = bitTheme.Color.SevereWarning.DarkHover ?? other.Color.SevereWarning.DarkHover;
        result.Color.SevereWarning.DarkActive = bitTheme.Color.SevereWarning.DarkActive ?? other.Color.SevereWarning.DarkActive;
        result.Color.SevereWarning.Light = bitTheme.Color.SevereWarning.Light ?? other.Color.SevereWarning.Light;
        result.Color.SevereWarning.LightHover = bitTheme.Color.SevereWarning.LightHover ?? other.Color.SevereWarning.LightHover;
        result.Color.SevereWarning.LightActive = bitTheme.Color.SevereWarning.LightActive ?? other.Color.SevereWarning.LightActive;
        result.Color.SevereWarning.Text = bitTheme.Color.SevereWarning.Text ?? other.Color.SevereWarning.Text;
        result.Color.SevereWarning.Disabled = bitTheme.Color.SevereWarning.Disabled ?? other.Color.SevereWarning.Disabled;
        result.Color.SevereWarning.DisabledText = bitTheme.Color.SevereWarning.DisabledText ?? other.Color.SevereWarning.DisabledText;
        result.Color.SevereWarning.Focus = bitTheme.Color.SevereWarning.Focus ?? other.Color.SevereWarning.Focus;

        result.Color.Error.Main = bitTheme.Color.Error.Main ?? other.Color.Error.Main;
        result.Color.Error.MainHover = bitTheme.Color.Error.MainHover ?? other.Color.Error.MainHover;
        result.Color.Error.MainActive = bitTheme.Color.Error.MainActive ?? other.Color.Error.MainActive;
        result.Color.Error.Dark = bitTheme.Color.Error.Dark ?? other.Color.Error.Dark;
        result.Color.Error.DarkHover = bitTheme.Color.Error.DarkHover ?? other.Color.Error.DarkHover;
        result.Color.Error.DarkActive = bitTheme.Color.Error.DarkActive ?? other.Color.Error.DarkActive;
        result.Color.Error.Light = bitTheme.Color.Error.Light ?? other.Color.Error.Light;
        result.Color.Error.LightHover = bitTheme.Color.Error.LightHover ?? other.Color.Error.LightHover;
        result.Color.Error.LightActive = bitTheme.Color.Error.LightActive ?? other.Color.Error.LightActive;
        result.Color.Error.Text = bitTheme.Color.Error.Text ?? other.Color.Error.Text;
        result.Color.Error.Disabled = bitTheme.Color.Error.Disabled ?? other.Color.Error.Disabled;
        result.Color.Error.DisabledText = bitTheme.Color.Error.DisabledText ?? other.Color.Error.DisabledText;
        result.Color.Error.Focus = bitTheme.Color.Error.Focus ?? other.Color.Error.Focus;

        result.Color.Foreground.Primary = bitTheme.Color.Foreground.Primary ?? other.Color.Foreground.Primary;
        result.Color.Foreground.PrimaryHover = bitTheme.Color.Foreground.PrimaryHover ?? other.Color.Foreground.PrimaryHover;
        result.Color.Foreground.PrimaryActive = bitTheme.Color.Foreground.PrimaryActive ?? other.Color.Foreground.PrimaryActive;
        result.Color.Foreground.PrimaryDark = bitTheme.Color.Foreground.PrimaryDark ?? other.Color.Foreground.PrimaryDark;
        result.Color.Foreground.PrimaryDarkHover = bitTheme.Color.Foreground.PrimaryDarkHover ?? other.Color.Foreground.PrimaryDarkHover;
        result.Color.Foreground.PrimaryDarkActive = bitTheme.Color.Foreground.PrimaryDarkActive ?? other.Color.Foreground.PrimaryDarkActive;
        result.Color.Foreground.PrimaryLight = bitTheme.Color.Foreground.PrimaryLight ?? other.Color.Foreground.PrimaryLight;
        result.Color.Foreground.PrimaryLightHover = bitTheme.Color.Foreground.PrimaryLightHover ?? other.Color.Foreground.PrimaryLightHover;
        result.Color.Foreground.PrimaryLightActive = bitTheme.Color.Foreground.PrimaryLightActive ?? other.Color.Foreground.PrimaryLightActive;
        result.Color.Foreground.PrimaryDisabled = bitTheme.Color.Foreground.PrimaryDisabled ?? other.Color.Foreground.PrimaryDisabled;
        result.Color.Foreground.PrimaryDisabledText = bitTheme.Color.Foreground.PrimaryDisabledText ?? other.Color.Foreground.PrimaryDisabledText;
        result.Color.Foreground.PrimaryFocus = bitTheme.Color.Foreground.PrimaryFocus ?? other.Color.Foreground.PrimaryFocus;
        result.Color.Foreground.Secondary = bitTheme.Color.Foreground.Secondary ?? other.Color.Foreground.Secondary;
        result.Color.Foreground.SecondaryHover = bitTheme.Color.Foreground.SecondaryHover ?? other.Color.Foreground.SecondaryHover;
        result.Color.Foreground.SecondaryActive = bitTheme.Color.Foreground.SecondaryActive ?? other.Color.Foreground.SecondaryActive;
        result.Color.Foreground.SecondaryDark = bitTheme.Color.Foreground.SecondaryDark ?? other.Color.Foreground.SecondaryDark;
        result.Color.Foreground.SecondaryDarkHover = bitTheme.Color.Foreground.SecondaryDarkHover ?? other.Color.Foreground.SecondaryDarkHover;
        result.Color.Foreground.SecondaryDarkActive = bitTheme.Color.Foreground.SecondaryDarkActive ?? other.Color.Foreground.SecondaryDarkActive;
        result.Color.Foreground.SecondaryLight = bitTheme.Color.Foreground.SecondaryLight ?? other.Color.Foreground.SecondaryLight;
        result.Color.Foreground.SecondaryLightHover = bitTheme.Color.Foreground.SecondaryLightHover ?? other.Color.Foreground.SecondaryLightHover;
        result.Color.Foreground.SecondaryLightActive = bitTheme.Color.Foreground.SecondaryLightActive ?? other.Color.Foreground.SecondaryLightActive;
        result.Color.Foreground.SecondaryDisabled = bitTheme.Color.Foreground.SecondaryDisabled ?? other.Color.Foreground.SecondaryDisabled;
        result.Color.Foreground.SecondaryDisabledText = bitTheme.Color.Foreground.SecondaryDisabledText ?? other.Color.Foreground.SecondaryDisabledText;
        result.Color.Foreground.SecondaryFocus = bitTheme.Color.Foreground.SecondaryFocus ?? other.Color.Foreground.SecondaryFocus;
        result.Color.Foreground.Tertiary = bitTheme.Color.Foreground.Tertiary ?? other.Color.Foreground.Tertiary;
        result.Color.Foreground.TertiaryHover = bitTheme.Color.Foreground.TertiaryHover ?? other.Color.Foreground.TertiaryHover;
        result.Color.Foreground.TertiaryActive = bitTheme.Color.Foreground.TertiaryActive ?? other.Color.Foreground.TertiaryActive;
        result.Color.Foreground.TertiaryDark = bitTheme.Color.Foreground.TertiaryDark ?? other.Color.Foreground.TertiaryDark;
        result.Color.Foreground.TertiaryDarkHover = bitTheme.Color.Foreground.TertiaryDarkHover ?? other.Color.Foreground.TertiaryDarkHover;
        result.Color.Foreground.TertiaryDarkActive = bitTheme.Color.Foreground.TertiaryDarkActive ?? other.Color.Foreground.TertiaryDarkActive;
        result.Color.Foreground.TertiaryLight = bitTheme.Color.Foreground.TertiaryLight ?? other.Color.Foreground.TertiaryLight;
        result.Color.Foreground.TertiaryLightHover = bitTheme.Color.Foreground.TertiaryLightHover ?? other.Color.Foreground.TertiaryLightHover;
        result.Color.Foreground.TertiaryLightActive = bitTheme.Color.Foreground.TertiaryLightActive ?? other.Color.Foreground.TertiaryLightActive;
        result.Color.Foreground.TertiaryDisabled = bitTheme.Color.Foreground.TertiaryDisabled ?? other.Color.Foreground.TertiaryDisabled;
        result.Color.Foreground.TertiaryDisabledText = bitTheme.Color.Foreground.TertiaryDisabledText ?? other.Color.Foreground.TertiaryDisabledText;
        result.Color.Foreground.TertiaryFocus = bitTheme.Color.Foreground.TertiaryFocus ?? other.Color.Foreground.TertiaryFocus;
        result.Color.Foreground.Disabled = bitTheme.Color.Foreground.Disabled ?? other.Color.Foreground.Disabled;

        result.Color.Background.Primary = bitTheme.Color.Background.Primary ?? other.Color.Background.Primary;
        result.Color.Background.PrimaryHover = bitTheme.Color.Background.PrimaryHover ?? other.Color.Background.PrimaryHover;
        result.Color.Background.PrimaryActive = bitTheme.Color.Background.PrimaryActive ?? other.Color.Background.PrimaryActive;
        result.Color.Background.PrimaryDark = bitTheme.Color.Background.PrimaryDark ?? other.Color.Background.PrimaryDark;
        result.Color.Background.PrimaryDarkHover = bitTheme.Color.Background.PrimaryDarkHover ?? other.Color.Background.PrimaryDarkHover;
        result.Color.Background.PrimaryDarkActive = bitTheme.Color.Background.PrimaryDarkActive ?? other.Color.Background.PrimaryDarkActive;
        result.Color.Background.PrimaryLight = bitTheme.Color.Background.PrimaryLight ?? other.Color.Background.PrimaryLight;
        result.Color.Background.PrimaryLightHover = bitTheme.Color.Background.PrimaryLightHover ?? other.Color.Background.PrimaryLightHover;
        result.Color.Background.PrimaryLightActive = bitTheme.Color.Background.PrimaryLightActive ?? other.Color.Background.PrimaryLightActive;
        result.Color.Background.PrimaryDisabled = bitTheme.Color.Background.PrimaryDisabled ?? other.Color.Background.PrimaryDisabled;
        result.Color.Background.PrimaryDisabledText = bitTheme.Color.Background.PrimaryDisabledText ?? other.Color.Background.PrimaryDisabledText;
        result.Color.Background.PrimaryFocus = bitTheme.Color.Background.PrimaryFocus ?? other.Color.Background.PrimaryFocus;
        result.Color.Background.Secondary = bitTheme.Color.Background.Secondary ?? other.Color.Background.Secondary;
        result.Color.Background.SecondaryHover = bitTheme.Color.Background.SecondaryHover ?? other.Color.Background.SecondaryHover;
        result.Color.Background.SecondaryActive = bitTheme.Color.Background.SecondaryActive ?? other.Color.Background.SecondaryActive;
        result.Color.Background.SecondaryDark = bitTheme.Color.Background.SecondaryDark ?? other.Color.Background.SecondaryDark;
        result.Color.Background.SecondaryDarkHover = bitTheme.Color.Background.SecondaryDarkHover ?? other.Color.Background.SecondaryDarkHover;
        result.Color.Background.SecondaryDarkActive = bitTheme.Color.Background.SecondaryDarkActive ?? other.Color.Background.SecondaryDarkActive;
        result.Color.Background.SecondaryLight = bitTheme.Color.Background.SecondaryLight ?? other.Color.Background.SecondaryLight;
        result.Color.Background.SecondaryLightHover = bitTheme.Color.Background.SecondaryLightHover ?? other.Color.Background.SecondaryLightHover;
        result.Color.Background.SecondaryLightActive = bitTheme.Color.Background.SecondaryLightActive ?? other.Color.Background.SecondaryLightActive;
        result.Color.Background.SecondaryDisabled = bitTheme.Color.Background.SecondaryDisabled ?? other.Color.Background.SecondaryDisabled;
        result.Color.Background.SecondaryDisabledText = bitTheme.Color.Background.SecondaryDisabledText ?? other.Color.Background.SecondaryDisabledText;
        result.Color.Background.SecondaryFocus = bitTheme.Color.Background.SecondaryFocus ?? other.Color.Background.SecondaryFocus;
        result.Color.Background.Tertiary = bitTheme.Color.Background.Tertiary ?? other.Color.Background.Tertiary;
        result.Color.Background.TertiaryHover = bitTheme.Color.Background.TertiaryHover ?? other.Color.Background.TertiaryHover;
        result.Color.Background.TertiaryActive = bitTheme.Color.Background.TertiaryActive ?? other.Color.Background.TertiaryActive;
        result.Color.Background.TertiaryDark = bitTheme.Color.Background.TertiaryDark ?? other.Color.Background.TertiaryDark;
        result.Color.Background.TertiaryDarkHover = bitTheme.Color.Background.TertiaryDarkHover ?? other.Color.Background.TertiaryDarkHover;
        result.Color.Background.TertiaryDarkActive = bitTheme.Color.Background.TertiaryDarkActive ?? other.Color.Background.TertiaryDarkActive;
        result.Color.Background.TertiaryLight = bitTheme.Color.Background.TertiaryLight ?? other.Color.Background.TertiaryLight;
        result.Color.Background.TertiaryLightHover = bitTheme.Color.Background.TertiaryLightHover ?? other.Color.Background.TertiaryLightHover;
        result.Color.Background.TertiaryLightActive = bitTheme.Color.Background.TertiaryLightActive ?? other.Color.Background.TertiaryLightActive;
        result.Color.Background.TertiaryDisabled = bitTheme.Color.Background.TertiaryDisabled ?? other.Color.Background.TertiaryDisabled;
        result.Color.Background.TertiaryDisabledText = bitTheme.Color.Background.TertiaryDisabledText ?? other.Color.Background.TertiaryDisabledText;
        result.Color.Background.TertiaryFocus = bitTheme.Color.Background.TertiaryFocus ?? other.Color.Background.TertiaryFocus;
        result.Color.Background.Disabled = bitTheme.Color.Background.Disabled ?? other.Color.Background.Disabled;
        result.Color.Background.Overlay = bitTheme.Color.Background.Overlay ?? other.Color.Background.Overlay;

        result.Color.Border.Primary = bitTheme.Color.Border.Primary ?? other.Color.Border.Primary;
        result.Color.Border.PrimaryHover = bitTheme.Color.Border.PrimaryHover ?? other.Color.Border.PrimaryHover;
        result.Color.Border.PrimaryActive = bitTheme.Color.Border.PrimaryActive ?? other.Color.Border.PrimaryActive;
        result.Color.Border.PrimaryDark = bitTheme.Color.Border.PrimaryDark ?? other.Color.Border.PrimaryDark;
        result.Color.Border.PrimaryDarkHover = bitTheme.Color.Border.PrimaryDarkHover ?? other.Color.Border.PrimaryDarkHover;
        result.Color.Border.PrimaryDarkActive = bitTheme.Color.Border.PrimaryDarkActive ?? other.Color.Border.PrimaryDarkActive;
        result.Color.Border.PrimaryLight = bitTheme.Color.Border.PrimaryLight ?? other.Color.Border.PrimaryLight;
        result.Color.Border.PrimaryLightHover = bitTheme.Color.Border.PrimaryLightHover ?? other.Color.Border.PrimaryLightHover;
        result.Color.Border.PrimaryLightActive = bitTheme.Color.Border.PrimaryLightActive ?? other.Color.Border.PrimaryLightActive;
        result.Color.Border.PrimaryDisabled = bitTheme.Color.Border.PrimaryDisabled ?? other.Color.Border.PrimaryDisabled;
        result.Color.Border.PrimaryDisabledText = bitTheme.Color.Border.PrimaryDisabledText ?? other.Color.Border.PrimaryDisabledText;
        result.Color.Border.PrimaryFocus = bitTheme.Color.Border.PrimaryFocus ?? other.Color.Border.PrimaryFocus;
        result.Color.Border.Secondary = bitTheme.Color.Border.Secondary ?? other.Color.Border.Secondary;
        result.Color.Border.SecondaryHover = bitTheme.Color.Border.SecondaryHover ?? other.Color.Border.SecondaryHover;
        result.Color.Border.SecondaryActive = bitTheme.Color.Border.SecondaryActive ?? other.Color.Border.SecondaryActive;
        result.Color.Border.SecondaryDark = bitTheme.Color.Border.SecondaryDark ?? other.Color.Border.SecondaryDark;
        result.Color.Border.SecondaryDarkHover = bitTheme.Color.Border.SecondaryDarkHover ?? other.Color.Border.SecondaryDarkHover;
        result.Color.Border.SecondaryDarkActive = bitTheme.Color.Border.SecondaryDarkActive ?? other.Color.Border.SecondaryDarkActive;
        result.Color.Border.SecondaryLight = bitTheme.Color.Border.SecondaryLight ?? other.Color.Border.SecondaryLight;
        result.Color.Border.SecondaryLightHover = bitTheme.Color.Border.SecondaryLightHover ?? other.Color.Border.SecondaryLightHover;
        result.Color.Border.SecondaryLightActive = bitTheme.Color.Border.SecondaryLightActive ?? other.Color.Border.SecondaryLightActive;
        result.Color.Border.SecondaryDisabled = bitTheme.Color.Border.SecondaryDisabled ?? other.Color.Border.SecondaryDisabled;
        result.Color.Border.SecondaryDisabledText = bitTheme.Color.Border.SecondaryDisabledText ?? other.Color.Border.SecondaryDisabledText;
        result.Color.Border.SecondaryFocus = bitTheme.Color.Border.SecondaryFocus ?? other.Color.Border.SecondaryFocus;
        result.Color.Border.Tertiary = bitTheme.Color.Border.Tertiary ?? other.Color.Border.Tertiary;
        result.Color.Border.TertiaryHover = bitTheme.Color.Border.TertiaryHover ?? other.Color.Border.TertiaryHover;
        result.Color.Border.TertiaryActive = bitTheme.Color.Border.TertiaryActive ?? other.Color.Border.TertiaryActive;
        result.Color.Border.TertiaryDark = bitTheme.Color.Border.TertiaryDark ?? other.Color.Border.TertiaryDark;
        result.Color.Border.TertiaryDarkHover = bitTheme.Color.Border.TertiaryDarkHover ?? other.Color.Border.TertiaryDarkHover;
        result.Color.Border.TertiaryDarkActive = bitTheme.Color.Border.TertiaryDarkActive ?? other.Color.Border.TertiaryDarkActive;
        result.Color.Border.TertiaryLight = bitTheme.Color.Border.TertiaryLight ?? other.Color.Border.TertiaryLight;
        result.Color.Border.TertiaryLightHover = bitTheme.Color.Border.TertiaryLightHover ?? other.Color.Border.TertiaryLightHover;
        result.Color.Border.TertiaryLightActive = bitTheme.Color.Border.TertiaryLightActive ?? other.Color.Border.TertiaryLightActive;
        result.Color.Border.TertiaryDisabled = bitTheme.Color.Border.TertiaryDisabled ?? other.Color.Border.TertiaryDisabled;
        result.Color.Border.TertiaryDisabledText = bitTheme.Color.Border.TertiaryDisabledText ?? other.Color.Border.TertiaryDisabledText;
        result.Color.Border.TertiaryFocus = bitTheme.Color.Border.TertiaryFocus ?? other.Color.Border.TertiaryFocus;
        result.Color.Border.Disabled = bitTheme.Color.Border.Disabled ?? other.Color.Border.Disabled;

        result.Color.Required = bitTheme.Color.Required ?? other.Color.Required;

        result.Color.Neutral.White = bitTheme.Color.Neutral.White ?? other.Color.Neutral.White;
        result.Color.Neutral.Black = bitTheme.Color.Neutral.Black ?? other.Color.Neutral.Black;
        result.Color.Neutral.Gray10 = bitTheme.Color.Neutral.Gray10 ?? other.Color.Neutral.Gray10;
        result.Color.Neutral.Gray20 = bitTheme.Color.Neutral.Gray20 ?? other.Color.Neutral.Gray20;
        result.Color.Neutral.Gray30 = bitTheme.Color.Neutral.Gray30 ?? other.Color.Neutral.Gray30;
        result.Color.Neutral.Gray40 = bitTheme.Color.Neutral.Gray40 ?? other.Color.Neutral.Gray40;
        result.Color.Neutral.Gray50 = bitTheme.Color.Neutral.Gray50 ?? other.Color.Neutral.Gray50;
        result.Color.Neutral.Gray60 = bitTheme.Color.Neutral.Gray60 ?? other.Color.Neutral.Gray60;
        result.Color.Neutral.Gray70 = bitTheme.Color.Neutral.Gray70 ?? other.Color.Neutral.Gray70;
        result.Color.Neutral.Gray80 = bitTheme.Color.Neutral.Gray80 ?? other.Color.Neutral.Gray80;
        result.Color.Neutral.Gray90 = bitTheme.Color.Neutral.Gray90 ?? other.Color.Neutral.Gray90;
        result.Color.Neutral.Gray100 = bitTheme.Color.Neutral.Gray100 ?? other.Color.Neutral.Gray100;
        result.Color.Neutral.Gray110 = bitTheme.Color.Neutral.Gray110 ?? other.Color.Neutral.Gray110;
        result.Color.Neutral.Gray120 = bitTheme.Color.Neutral.Gray120 ?? other.Color.Neutral.Gray120;
        result.Color.Neutral.Gray130 = bitTheme.Color.Neutral.Gray130 ?? other.Color.Neutral.Gray130;
        result.Color.Neutral.Gray140 = bitTheme.Color.Neutral.Gray140 ?? other.Color.Neutral.Gray140;
        result.Color.Neutral.Gray150 = bitTheme.Color.Neutral.Gray150 ?? other.Color.Neutral.Gray150;
        result.Color.Neutral.Gray160 = bitTheme.Color.Neutral.Gray160 ?? other.Color.Neutral.Gray160;
        result.Color.Neutral.Gray170 = bitTheme.Color.Neutral.Gray170 ?? other.Color.Neutral.Gray170;
        result.Color.Neutral.Gray180 = bitTheme.Color.Neutral.Gray180 ?? other.Color.Neutral.Gray180;
        result.Color.Neutral.Gray190 = bitTheme.Color.Neutral.Gray190 ?? other.Color.Neutral.Gray190;
        result.Color.Neutral.Gray200 = bitTheme.Color.Neutral.Gray200 ?? other.Color.Neutral.Gray200;
        result.Color.Neutral.Gray210 = bitTheme.Color.Neutral.Gray210 ?? other.Color.Neutral.Gray210;
        result.Color.Neutral.Gray220 = bitTheme.Color.Neutral.Gray220 ?? other.Color.Neutral.Gray220;

        result.Color.Semantic.SurfacePage = bitTheme.Color.Semantic.SurfacePage ?? other.Color.Semantic.SurfacePage;
        result.Color.Semantic.SurfaceElevated = bitTheme.Color.Semantic.SurfaceElevated ?? other.Color.Semantic.SurfaceElevated;
        result.Color.Semantic.SurfaceMuted = bitTheme.Color.Semantic.SurfaceMuted ?? other.Color.Semantic.SurfaceMuted;
        result.Color.Semantic.TextPrimary = bitTheme.Color.Semantic.TextPrimary ?? other.Color.Semantic.TextPrimary;
        result.Color.Semantic.TextSecondary = bitTheme.Color.Semantic.TextSecondary ?? other.Color.Semantic.TextSecondary;
        result.Color.Semantic.BorderDefault = bitTheme.Color.Semantic.BorderDefault ?? other.Color.Semantic.BorderDefault;
        result.Color.Semantic.AccentPrimary = bitTheme.Color.Semantic.AccentPrimary ?? other.Color.Semantic.AccentPrimary;
        result.Color.Semantic.FocusRing = bitTheme.Color.Semantic.FocusRing ?? other.Color.Semantic.FocusRing;
        result.Color.Semantic.FocusColor = bitTheme.Color.Semantic.FocusColor ?? other.Color.Semantic.FocusColor;

        result.BoxShadow.Callout = bitTheme.BoxShadow.Callout ?? other.BoxShadow.Callout;
        result.BoxShadow.Callout2 = bitTheme.BoxShadow.Callout2 ?? other.BoxShadow.Callout2;
        result.BoxShadow.Sm = bitTheme.BoxShadow.Sm ?? other.BoxShadow.Sm;
        result.BoxShadow.Nm = bitTheme.BoxShadow.Nm ?? other.BoxShadow.Nm;
        result.BoxShadow.Md = bitTheme.BoxShadow.Md ?? other.BoxShadow.Md;
        result.BoxShadow.Lg = bitTheme.BoxShadow.Lg ?? other.BoxShadow.Lg;
        result.BoxShadow.Xl = bitTheme.BoxShadow.Xl ?? other.BoxShadow.Xl;
        result.BoxShadow.Xxl = bitTheme.BoxShadow.Xxl ?? other.BoxShadow.Xxl;
        result.BoxShadow.Inner = bitTheme.BoxShadow.Inner ?? other.BoxShadow.Inner;
        result.BoxShadow.S1 = bitTheme.BoxShadow.S1 ?? other.BoxShadow.S1;
        result.BoxShadow.S2 = bitTheme.BoxShadow.S2 ?? other.BoxShadow.S2;
        result.BoxShadow.S3 = bitTheme.BoxShadow.S3 ?? other.BoxShadow.S3;
        result.BoxShadow.S4 = bitTheme.BoxShadow.S4 ?? other.BoxShadow.S4;
        result.BoxShadow.S5 = bitTheme.BoxShadow.S5 ?? other.BoxShadow.S5;
        result.BoxShadow.S6 = bitTheme.BoxShadow.S6 ?? other.BoxShadow.S6;
        result.BoxShadow.S7 = bitTheme.BoxShadow.S7 ?? other.BoxShadow.S7;
        result.BoxShadow.S8 = bitTheme.BoxShadow.S8 ?? other.BoxShadow.S8;
        result.BoxShadow.S9 = bitTheme.BoxShadow.S9 ?? other.BoxShadow.S9;
        result.BoxShadow.S10 = bitTheme.BoxShadow.S10 ?? other.BoxShadow.S10;
        result.BoxShadow.S11 = bitTheme.BoxShadow.S11 ?? other.BoxShadow.S11;
        result.BoxShadow.S12 = bitTheme.BoxShadow.S12 ?? other.BoxShadow.S12;
        result.BoxShadow.S13 = bitTheme.BoxShadow.S13 ?? other.BoxShadow.S13;
        result.BoxShadow.S14 = bitTheme.BoxShadow.S14 ?? other.BoxShadow.S14;
        result.BoxShadow.S15 = bitTheme.BoxShadow.S15 ?? other.BoxShadow.S15;
        result.BoxShadow.S16 = bitTheme.BoxShadow.S16 ?? other.BoxShadow.S16;
        result.BoxShadow.S17 = bitTheme.BoxShadow.S17 ?? other.BoxShadow.S17;
        result.BoxShadow.S18 = bitTheme.BoxShadow.S18 ?? other.BoxShadow.S18;
        result.BoxShadow.S19 = bitTheme.BoxShadow.S19 ?? other.BoxShadow.S19;
        result.BoxShadow.S20 = bitTheme.BoxShadow.S20 ?? other.BoxShadow.S20;
        result.BoxShadow.S21 = bitTheme.BoxShadow.S21 ?? other.BoxShadow.S21;
        result.BoxShadow.S22 = bitTheme.BoxShadow.S22 ?? other.BoxShadow.S22;
        result.BoxShadow.S23 = bitTheme.BoxShadow.S23 ?? other.BoxShadow.S23;
        result.BoxShadow.S24 = bitTheme.BoxShadow.S24 ?? other.BoxShadow.S24;
        result.BoxShadow.FocusRing = bitTheme.BoxShadow.FocusRing ?? other.BoxShadow.FocusRing;
        result.BoxShadow.Card = bitTheme.BoxShadow.Card ?? other.BoxShadow.Card;
        result.BoxShadow.Popup = bitTheme.BoxShadow.Popup ?? other.BoxShadow.Popup;
        result.BoxShadow.Dialog = bitTheme.BoxShadow.Dialog ?? other.BoxShadow.Dialog;
        result.BoxShadow.Sheet = bitTheme.BoxShadow.Sheet ?? other.BoxShadow.Sheet;
        result.BoxShadow.Tooltip = bitTheme.BoxShadow.Tooltip ?? other.BoxShadow.Tooltip;
        result.BoxShadow.Snackbar = bitTheme.BoxShadow.Snackbar ?? other.BoxShadow.Snackbar;
        result.BoxShadow.AppBarTop = bitTheme.BoxShadow.AppBarTop ?? other.BoxShadow.AppBarTop;
        result.BoxShadow.AppBarBottom = bitTheme.BoxShadow.AppBarBottom ?? other.BoxShadow.AppBarBottom;

        result.Spacing.ScalingFactor = bitTheme.Spacing.ScalingFactor ?? other.Spacing.ScalingFactor;
        result.Spacing.Dialog = bitTheme.Spacing.Dialog ?? other.Spacing.Dialog;

        result.ZIndex.Snackbar = bitTheme.ZIndex.Snackbar ?? other.ZIndex.Snackbar;
        result.ZIndex.Modal = bitTheme.ZIndex.Modal ?? other.ZIndex.Modal;
        result.ZIndex.Callout = bitTheme.ZIndex.Callout ?? other.ZIndex.Callout;
        result.ZIndex.Overlay = bitTheme.ZIndex.Overlay ?? other.ZIndex.Overlay;
        result.ZIndex.Base = bitTheme.ZIndex.Base ?? other.ZIndex.Base;

        result.Shape.BorderRadius = bitTheme.Shape.BorderRadius ?? other.Shape.BorderRadius;
        result.Shape.BorderWidth = bitTheme.Shape.BorderWidth ?? other.Shape.BorderWidth;
        result.Shape.BorderStyle = bitTheme.Shape.BorderStyle ?? other.Shape.BorderStyle;
        result.Shape.FocusRingWidth = bitTheme.Shape.FocusRingWidth ?? other.Shape.FocusRingWidth;
        result.Shape.FocusRingOffset = bitTheme.Shape.FocusRingOffset ?? other.Shape.FocusRingOffset;
        result.Shape.BorderWidthThick = bitTheme.Shape.BorderWidthThick ?? other.Shape.BorderWidthThick;
        result.Shape.Radius.None = bitTheme.Shape.Radius.None ?? other.Shape.Radius.None;
        result.Shape.Radius.Xs = bitTheme.Shape.Radius.Xs ?? other.Shape.Radius.Xs;
        result.Shape.Radius.Sm = bitTheme.Shape.Radius.Sm ?? other.Shape.Radius.Sm;
        result.Shape.Radius.Md = bitTheme.Shape.Radius.Md ?? other.Shape.Radius.Md;
        result.Shape.Radius.Lg = bitTheme.Shape.Radius.Lg ?? other.Shape.Radius.Lg;
        result.Shape.Radius.Xl = bitTheme.Shape.Radius.Xl ?? other.Shape.Radius.Xl;
        result.Shape.Radius.Xxl = bitTheme.Shape.Radius.Xxl ?? other.Shape.Radius.Xxl;
        result.Shape.Radius.Full = bitTheme.Shape.Radius.Full ?? other.Shape.Radius.Full;
        result.Shape.Radius.Control = bitTheme.Shape.Radius.Control ?? other.Shape.Radius.Control;
        result.Shape.Radius.Button = bitTheme.Shape.Radius.Button ?? other.Shape.Radius.Button;
        result.Shape.Radius.Chip = bitTheme.Shape.Radius.Chip ?? other.Shape.Radius.Chip;
        result.Shape.Radius.Selection = bitTheme.Shape.Radius.Selection ?? other.Shape.Radius.Selection;
        result.Shape.Radius.Surface = bitTheme.Shape.Radius.Surface ?? other.Shape.Radius.Surface;
        result.Shape.Radius.Popup = bitTheme.Shape.Radius.Popup ?? other.Shape.Radius.Popup;
        result.Shape.Radius.Dialog = bitTheme.Shape.Radius.Dialog ?? other.Shape.Radius.Dialog;

        result.Typography.FontFamily = bitTheme.Typography.FontFamily ?? other.Typography.FontFamily;
        result.Typography.MonoFontFamily = bitTheme.Typography.MonoFontFamily ?? other.Typography.MonoFontFamily;
        result.Typography.FontWeight = bitTheme.Typography.FontWeight ?? other.Typography.FontWeight;
        result.Typography.LineHeight = bitTheme.Typography.LineHeight ?? other.Typography.LineHeight;
        result.Typography.GutterSize = bitTheme.Typography.GutterSize ?? other.Typography.GutterSize;

        result.Typography.FontSize.Xxs = bitTheme.Typography.FontSize.Xxs ?? other.Typography.FontSize.Xxs;
        result.Typography.FontSize.Xs = bitTheme.Typography.FontSize.Xs ?? other.Typography.FontSize.Xs;
        result.Typography.FontSize.Sm = bitTheme.Typography.FontSize.Sm ?? other.Typography.FontSize.Sm;
        result.Typography.FontSize.Md = bitTheme.Typography.FontSize.Md ?? other.Typography.FontSize.Md;
        result.Typography.FontSize.Lg = bitTheme.Typography.FontSize.Lg ?? other.Typography.FontSize.Lg;
        result.Typography.FontSize.Xl = bitTheme.Typography.FontSize.Xl ?? other.Typography.FontSize.Xl;
        result.Typography.FontSize.Xxl = bitTheme.Typography.FontSize.Xxl ?? other.Typography.FontSize.Xxl;
        result.Typography.FontSize.Xxxl = bitTheme.Typography.FontSize.Xxxl ?? other.Typography.FontSize.Xxxl;
        result.Typography.FontSize.Xxxxl = bitTheme.Typography.FontSize.Xxxxl ?? other.Typography.FontSize.Xxxxl;

        result.Typography.FontWeights.Light = bitTheme.Typography.FontWeights.Light ?? other.Typography.FontWeights.Light;
        result.Typography.FontWeights.Regular = bitTheme.Typography.FontWeights.Regular ?? other.Typography.FontWeights.Regular;
        result.Typography.FontWeights.Medium = bitTheme.Typography.FontWeights.Medium ?? other.Typography.FontWeights.Medium;
        result.Typography.FontWeights.SemiBold = bitTheme.Typography.FontWeights.SemiBold ?? other.Typography.FontWeights.SemiBold;
        result.Typography.FontWeights.Bold = bitTheme.Typography.FontWeights.Bold ?? other.Typography.FontWeights.Bold;

        result.Typography.Control.LetterSpacing = bitTheme.Typography.Control.LetterSpacing ?? other.Typography.Control.LetterSpacing;
        result.Typography.Control.TextTransform = bitTheme.Typography.Control.TextTransform ?? other.Typography.Control.TextTransform;

        result.Typography.Body1.Margin = bitTheme.Typography.Body1.Margin ?? other.Typography.Body1.Margin;
        result.Typography.Body1.FontWeight = bitTheme.Typography.Body1.FontWeight ?? other.Typography.Body1.FontWeight;
        result.Typography.Body1.FontSize = bitTheme.Typography.Body1.FontSize ?? other.Typography.Body1.FontSize;
        result.Typography.Body1.LineHeight = bitTheme.Typography.Body1.LineHeight ?? other.Typography.Body1.LineHeight;
        result.Typography.Body1.LetterSpacing = bitTheme.Typography.Body1.LetterSpacing ?? other.Typography.Body1.LetterSpacing;

        result.Typography.Body2.Margin = bitTheme.Typography.Body2.Margin ?? other.Typography.Body2.Margin;
        result.Typography.Body2.FontWeight = bitTheme.Typography.Body2.FontWeight ?? other.Typography.Body2.FontWeight;
        result.Typography.Body2.FontSize = bitTheme.Typography.Body2.FontSize ?? other.Typography.Body2.FontSize;
        result.Typography.Body2.LineHeight = bitTheme.Typography.Body2.LineHeight ?? other.Typography.Body2.LineHeight;
        result.Typography.Body2.LetterSpacing = bitTheme.Typography.Body2.LetterSpacing ?? other.Typography.Body2.LetterSpacing;

        result.Typography.Button.Margin = bitTheme.Typography.Button.Margin ?? other.Typography.Button.Margin;
        result.Typography.Button.FontWeight = bitTheme.Typography.Button.FontWeight ?? other.Typography.Button.FontWeight;
        result.Typography.Button.FontSize = bitTheme.Typography.Button.FontSize ?? other.Typography.Button.FontSize;
        result.Typography.Button.LineHeight = bitTheme.Typography.Button.LineHeight ?? other.Typography.Button.LineHeight;
        result.Typography.Button.LetterSpacing = bitTheme.Typography.Button.LetterSpacing ?? other.Typography.Button.LetterSpacing;
        result.Typography.Button.TextTransform = bitTheme.Typography.Button.TextTransform ?? other.Typography.Button.TextTransform;
        result.Typography.Button.Display = bitTheme.Typography.Button.Display ?? other.Typography.Button.Display;

        result.Typography.Caption1.Margin = bitTheme.Typography.Caption1.Margin ?? other.Typography.Caption1.Margin;
        result.Typography.Caption1.FontWeight = bitTheme.Typography.Caption1.FontWeight ?? other.Typography.Caption1.FontWeight;
        result.Typography.Caption1.FontSize = bitTheme.Typography.Caption1.FontSize ?? other.Typography.Caption1.FontSize;
        result.Typography.Caption1.LineHeight = bitTheme.Typography.Caption1.LineHeight ?? other.Typography.Caption1.LineHeight;
        result.Typography.Caption1.LetterSpacing = bitTheme.Typography.Caption1.LetterSpacing ?? other.Typography.Caption1.LetterSpacing;

        result.Typography.Caption2.Margin = bitTheme.Typography.Caption2.Margin ?? other.Typography.Caption2.Margin;
        result.Typography.Caption2.FontWeight = bitTheme.Typography.Caption2.FontWeight ?? other.Typography.Caption2.FontWeight;
        result.Typography.Caption2.FontSize = bitTheme.Typography.Caption2.FontSize ?? other.Typography.Caption2.FontSize;
        result.Typography.Caption2.LineHeight = bitTheme.Typography.Caption2.LineHeight ?? other.Typography.Caption2.LineHeight;
        result.Typography.Caption2.LetterSpacing = bitTheme.Typography.Caption2.LetterSpacing ?? other.Typography.Caption2.LetterSpacing;

        result.Typography.H1.Margin = bitTheme.Typography.H1.Margin ?? other.Typography.H1.Margin;
        result.Typography.H1.FontWeight = bitTheme.Typography.H1.FontWeight ?? other.Typography.H1.FontWeight;
        result.Typography.H1.FontSize = bitTheme.Typography.H1.FontSize ?? other.Typography.H1.FontSize;
        result.Typography.H1.LineHeight = bitTheme.Typography.H1.LineHeight ?? other.Typography.H1.LineHeight;
        result.Typography.H1.LetterSpacing = bitTheme.Typography.H1.LetterSpacing ?? other.Typography.H1.LetterSpacing;

        result.Typography.H2.Margin = bitTheme.Typography.H2.Margin ?? other.Typography.H2.Margin;
        result.Typography.H2.FontWeight = bitTheme.Typography.H2.FontWeight ?? other.Typography.H2.FontWeight;
        result.Typography.H2.FontSize = bitTheme.Typography.H2.FontSize ?? other.Typography.H2.FontSize;
        result.Typography.H2.LineHeight = bitTheme.Typography.H2.LineHeight ?? other.Typography.H2.LineHeight;
        result.Typography.H2.LetterSpacing = bitTheme.Typography.H2.LetterSpacing ?? other.Typography.H2.LetterSpacing;

        result.Typography.H3.Margin = bitTheme.Typography.H3.Margin ?? other.Typography.H3.Margin;
        result.Typography.H3.FontWeight = bitTheme.Typography.H3.FontWeight ?? other.Typography.H3.FontWeight;
        result.Typography.H3.FontSize = bitTheme.Typography.H3.FontSize ?? other.Typography.H3.FontSize;
        result.Typography.H3.LineHeight = bitTheme.Typography.H3.LineHeight ?? other.Typography.H3.LineHeight;
        result.Typography.H3.LetterSpacing = bitTheme.Typography.H3.LetterSpacing ?? other.Typography.H3.LetterSpacing;

        result.Typography.H4.Margin = bitTheme.Typography.H4.Margin ?? other.Typography.H4.Margin;
        result.Typography.H4.FontWeight = bitTheme.Typography.H4.FontWeight ?? other.Typography.H4.FontWeight;
        result.Typography.H4.FontSize = bitTheme.Typography.H4.FontSize ?? other.Typography.H4.FontSize;
        result.Typography.H4.LineHeight = bitTheme.Typography.H4.LineHeight ?? other.Typography.H4.LineHeight;
        result.Typography.H4.LetterSpacing = bitTheme.Typography.H4.LetterSpacing ?? other.Typography.H4.LetterSpacing;

        result.Typography.H5.Margin = bitTheme.Typography.H5.Margin ?? other.Typography.H5.Margin;
        result.Typography.H5.FontWeight = bitTheme.Typography.H5.FontWeight ?? other.Typography.H5.FontWeight;
        result.Typography.H5.FontSize = bitTheme.Typography.H5.FontSize ?? other.Typography.H5.FontSize;
        result.Typography.H5.LineHeight = bitTheme.Typography.H5.LineHeight ?? other.Typography.H5.LineHeight;
        result.Typography.H5.LetterSpacing = bitTheme.Typography.H5.LetterSpacing ?? other.Typography.H5.LetterSpacing;

        result.Typography.H6.Margin = bitTheme.Typography.H6.Margin ?? other.Typography.H6.Margin;
        result.Typography.H6.FontWeight = bitTheme.Typography.H6.FontWeight ?? other.Typography.H6.FontWeight;
        result.Typography.H6.FontSize = bitTheme.Typography.H6.FontSize ?? other.Typography.H6.FontSize;
        result.Typography.H6.LineHeight = bitTheme.Typography.H6.LineHeight ?? other.Typography.H6.LineHeight;
        result.Typography.H6.LetterSpacing = bitTheme.Typography.H6.LetterSpacing ?? other.Typography.H6.LetterSpacing;

        result.Typography.Inherit.Margin = bitTheme.Typography.Inherit.Margin ?? other.Typography.Inherit.Margin;
        result.Typography.Inherit.FontFamily = bitTheme.Typography.Inherit.FontFamily ?? other.Typography.Inherit.FontFamily;
        result.Typography.Inherit.FontWeight = bitTheme.Typography.Inherit.FontWeight ?? other.Typography.Inherit.FontWeight;
        result.Typography.Inherit.FontSize = bitTheme.Typography.Inherit.FontSize ?? other.Typography.Inherit.FontSize;
        result.Typography.Inherit.LineHeight = bitTheme.Typography.Inherit.LineHeight ?? other.Typography.Inherit.LineHeight;
        result.Typography.Inherit.LetterSpacing = bitTheme.Typography.Inherit.LetterSpacing ?? other.Typography.Inherit.LetterSpacing;
        result.Typography.Inherit.TextTransform = bitTheme.Typography.Inherit.TextTransform ?? other.Typography.Inherit.TextTransform;
        result.Typography.Inherit.Display = bitTheme.Typography.Inherit.Display ?? other.Typography.Inherit.Display;

        result.Typography.Overline.Margin = bitTheme.Typography.Overline.Margin ?? other.Typography.Overline.Margin;
        result.Typography.Overline.FontWeight = bitTheme.Typography.Overline.FontWeight ?? other.Typography.Overline.FontWeight;
        result.Typography.Overline.FontSize = bitTheme.Typography.Overline.FontSize ?? other.Typography.Overline.FontSize;
        result.Typography.Overline.LineHeight = bitTheme.Typography.Overline.LineHeight ?? other.Typography.Overline.LineHeight;
        result.Typography.Overline.LetterSpacing = bitTheme.Typography.Overline.LetterSpacing ?? other.Typography.Overline.LetterSpacing;
        result.Typography.Overline.TextTransform = bitTheme.Typography.Overline.TextTransform ?? other.Typography.Overline.TextTransform;
        result.Typography.Overline.Display = bitTheme.Typography.Overline.Display ?? other.Typography.Overline.Display;

        result.Typography.Subtitle1.Margin = bitTheme.Typography.Subtitle1.Margin ?? other.Typography.Subtitle1.Margin;
        result.Typography.Subtitle1.FontWeight = bitTheme.Typography.Subtitle1.FontWeight ?? other.Typography.Subtitle1.FontWeight;
        result.Typography.Subtitle1.FontSize = bitTheme.Typography.Subtitle1.FontSize ?? other.Typography.Subtitle1.FontSize;
        result.Typography.Subtitle1.LineHeight = bitTheme.Typography.Subtitle1.LineHeight ?? other.Typography.Subtitle1.LineHeight;
        result.Typography.Subtitle1.LetterSpacing = bitTheme.Typography.Subtitle1.LetterSpacing ?? other.Typography.Subtitle1.LetterSpacing;

        result.Typography.Subtitle2.Margin = bitTheme.Typography.Subtitle2.Margin ?? other.Typography.Subtitle2.Margin;
        result.Typography.Subtitle2.FontWeight = bitTheme.Typography.Subtitle2.FontWeight ?? other.Typography.Subtitle2.FontWeight;
        result.Typography.Subtitle2.FontSize = bitTheme.Typography.Subtitle2.FontSize ?? other.Typography.Subtitle2.FontSize;
        result.Typography.Subtitle2.LineHeight = bitTheme.Typography.Subtitle2.LineHeight ?? other.Typography.Subtitle2.LineHeight;
        result.Typography.Subtitle2.LetterSpacing = bitTheme.Typography.Subtitle2.LetterSpacing ?? other.Typography.Subtitle2.LetterSpacing;

        result.Motion.Duration = bitTheme.Motion.Duration ?? other.Motion.Duration;
        result.Motion.DurationShort = bitTheme.Motion.DurationShort ?? other.Motion.DurationShort;
        result.Motion.DurationLong = bitTheme.Motion.DurationLong ?? other.Motion.DurationLong;
        result.Motion.EasingStandard = bitTheme.Motion.EasingStandard ?? other.Motion.EasingStandard;
        result.Motion.EasingDecelerate = bitTheme.Motion.EasingDecelerate ?? other.Motion.EasingDecelerate;
        result.Motion.EasingAccelerate = bitTheme.Motion.EasingAccelerate ?? other.Motion.EasingAccelerate;
        result.Motion.DurationSpinner = bitTheme.Motion.DurationSpinner ?? other.Motion.DurationSpinner;
        result.Motion.EasingSpinner = bitTheme.Motion.EasingSpinner ?? other.Motion.EasingSpinner;
        result.Motion.LoopFactor = bitTheme.Motion.LoopFactor ?? other.Motion.LoopFactor;

        result.Layout.DensityScale = bitTheme.Layout.DensityScale ?? other.Layout.DensityScale;
        result.Layout.DialogActionsDirection = bitTheme.Layout.DialogActionsDirection ?? other.Layout.DialogActionsDirection;
        result.Layout.DialogActionsJustify = bitTheme.Layout.DialogActionsJustify ?? other.Layout.DialogActionsJustify;
        result.Layout.DialogActionsAlign = bitTheme.Layout.DialogActionsAlign ?? other.Layout.DialogActionsAlign;
        result.Layout.Breakpoints.Xs = bitTheme.Layout.Breakpoints.Xs ?? other.Layout.Breakpoints.Xs;
        result.Layout.Breakpoints.Sm = bitTheme.Layout.Breakpoints.Sm ?? other.Layout.Breakpoints.Sm;
        result.Layout.Breakpoints.Md = bitTheme.Layout.Breakpoints.Md ?? other.Layout.Breakpoints.Md;
        result.Layout.Breakpoints.Lg = bitTheme.Layout.Breakpoints.Lg ?? other.Layout.Breakpoints.Lg;
        result.Layout.Breakpoints.Xl = bitTheme.Layout.Breakpoints.Xl ?? other.Layout.Breakpoints.Xl;
        result.Layout.Breakpoints.Xxl = bitTheme.Layout.Breakpoints.Xxl ?? other.Layout.Breakpoints.Xxl;

        result.Size.Control.Sm = bitTheme.Size.Control.Sm ?? other.Size.Control.Sm;
        result.Size.Control.Md = bitTheme.Size.Control.Md ?? other.Size.Control.Md;
        result.Size.Control.Lg = bitTheme.Size.Control.Lg ?? other.Size.Control.Lg;
        result.Size.ControlPaddingX.Sm = bitTheme.Size.ControlPaddingX.Sm ?? other.Size.ControlPaddingX.Sm;
        result.Size.ControlPaddingX.Md = bitTheme.Size.ControlPaddingX.Md ?? other.Size.ControlPaddingX.Md;
        result.Size.ControlPaddingX.Lg = bitTheme.Size.ControlPaddingX.Lg ?? other.Size.ControlPaddingX.Lg;
        result.Size.ControlPaddingY.Sm = bitTheme.Size.ControlPaddingY.Sm ?? other.Size.ControlPaddingY.Sm;
        result.Size.ControlPaddingY.Md = bitTheme.Size.ControlPaddingY.Md ?? other.Size.ControlPaddingY.Md;
        result.Size.ControlPaddingY.Lg = bitTheme.Size.ControlPaddingY.Lg ?? other.Size.ControlPaddingY.Lg;
        result.Size.ControlMinWidth = bitTheme.Size.ControlMinWidth ?? other.Size.ControlMinWidth;
        result.Size.Icon.Sm = bitTheme.Size.Icon.Sm ?? other.Size.Icon.Sm;
        result.Size.Icon.Md = bitTheme.Size.Icon.Md ?? other.Size.Icon.Md;
        result.Size.Icon.Lg = bitTheme.Size.Icon.Lg ?? other.Size.Icon.Lg;
        result.Size.Selection.Sm = bitTheme.Size.Selection.Sm ?? other.Size.Selection.Sm;
        result.Size.Selection.Md = bitTheme.Size.Selection.Md ?? other.Size.Selection.Md;
        result.Size.Selection.Lg = bitTheme.Size.Selection.Lg ?? other.Size.Selection.Lg;
        result.Size.Item.Sm = bitTheme.Size.Item.Sm ?? other.Size.Item.Sm;
        result.Size.Item.Md = bitTheme.Size.Item.Md ?? other.Size.Item.Md;
        result.Size.Item.Lg = bitTheme.Size.Item.Lg ?? other.Size.Item.Lg;
        result.Size.Tab = bitTheme.Size.Tab ?? other.Size.Tab;
        result.Size.TabIndicator = bitTheme.Size.TabIndicator ?? other.Size.TabIndicator;
        result.Size.Divider = bitTheme.Size.Divider ?? other.Size.Divider;
        result.Size.Track.Sm = bitTheme.Size.Track.Sm ?? other.Size.Track.Sm;
        result.Size.Track.Md = bitTheme.Size.Track.Md ?? other.Size.Track.Md;
        result.Size.Track.Lg = bitTheme.Size.Track.Lg ?? other.Size.Track.Lg;
        result.Size.Switch.Width.Sm = bitTheme.Size.Switch.Width.Sm ?? other.Size.Switch.Width.Sm;
        result.Size.Switch.Width.Md = bitTheme.Size.Switch.Width.Md ?? other.Size.Switch.Width.Md;
        result.Size.Switch.Width.Lg = bitTheme.Size.Switch.Width.Lg ?? other.Size.Switch.Width.Lg;
        result.Size.Switch.Height.Sm = bitTheme.Size.Switch.Height.Sm ?? other.Size.Switch.Height.Sm;
        result.Size.Switch.Height.Md = bitTheme.Size.Switch.Height.Md ?? other.Size.Switch.Height.Md;
        result.Size.Switch.Height.Lg = bitTheme.Size.Switch.Height.Lg ?? other.Size.Switch.Height.Lg;
        result.Size.Switch.Thumb.Sm = bitTheme.Size.Switch.Thumb.Sm ?? other.Size.Switch.Thumb.Sm;
        result.Size.Switch.Thumb.Md = bitTheme.Size.Switch.Thumb.Md ?? other.Size.Switch.Thumb.Md;
        result.Size.Switch.Thumb.Lg = bitTheme.Size.Switch.Thumb.Lg ?? other.Size.Switch.Thumb.Lg;
        result.Size.SliderThumb.Sm = bitTheme.Size.SliderThumb.Sm ?? other.Size.SliderThumb.Sm;
        result.Size.SliderThumb.Md = bitTheme.Size.SliderThumb.Md ?? other.Size.SliderThumb.Md;
        result.Size.SliderThumb.Lg = bitTheme.Size.SliderThumb.Lg ?? other.Size.SliderThumb.Lg;
        result.Size.SpinnerStroke = bitTheme.Size.SpinnerStroke ?? other.Size.SpinnerStroke;
        result.Size.PopupMaxHeight = bitTheme.Size.PopupMaxHeight ?? other.Size.PopupMaxHeight;
        result.Size.DialogMaxWidth = bitTheme.Size.DialogMaxWidth ?? other.Size.DialogMaxWidth;

        result.Opacity.Disabled = bitTheme.Opacity.Disabled ?? other.Opacity.Disabled;

        return result;
    }
}
