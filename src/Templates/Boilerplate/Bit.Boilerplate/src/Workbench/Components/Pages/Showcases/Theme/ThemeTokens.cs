namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme;

/// <summary>How the browser reads a token's value: a color as hex, a length in pixels, or anything else as written.</summary>
public enum ThemeTokenKind
{
    Color,
    Length,
    Raw
}

/// <summary>
/// One of the theme's --bit-* tokens. <see cref="Expression"/> is what the browser resolves for it, when that isn't the
/// token alone: a size the density scales is declared as steps of the spacing unit and is only worked out where it is used.
/// </summary>
public sealed record ThemeToken(string Name, ThemeTokenKind Kind = ThemeTokenKind.Raw, string? Expression = null)
{
    public static ThemeToken Color(string name) => new(name, ThemeTokenKind.Color);

    public static ThemeToken Length(string name) => new(name, ThemeTokenKind.Length);

    /// <summary>A size the density scales: the token when the theme sets it, else its steps times the spacing unit and the density.</summary>
    public static ThemeToken Scaled(string name) => new(name, ThemeTokenKind.Length, $"var({name}, calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * var({name}-steps)))");

    public static ThemeToken Raw(string name) => new(name);

    /// <summary>The CSS value that draws the token on a sample.</summary>
    public string Css => Expression ?? $"var({Name})";
}

/// <summary>A titled group of tokens; the title is English text the page localizes.</summary>
public sealed record ThemeTokenGroup(string TitleKey, ThemeToken[] Tokens);

/// <summary>What the page asks the browser to resolve for one token.</summary>
public sealed record ThemeTokenRequest(string Name, string Kind, string Value);

/// <summary>Every token family of the bit BlazorUI theme, grouped the way the Theme page shows them.</summary>
public static class ThemeTokens
{
    /// <summary>The slots of each color role: its main color, its states, its dark and light shades, its text, foreground, tint and disabled colors.</summary>
    private static readonly string[] roleSlots = ["", "-hover", "-active", "-focus", "-dark", "-dark-hover", "-dark-active", "-light", "-light-hover", "-light-active", "-text", "-fg", "-tint", "-dis", "-dis-text"];

    /// <summary>The color roles, each with the resource key of its name.</summary>
    public static readonly (string NameKey, ThemeToken[] Tokens)[] Roles =
    [
        .. new (string NameKey, string Code)[]
        {
            (nameof(WorkbenchStrings.ThemePrimary), "pri"),
            (nameof(WorkbenchStrings.ThemeSecondary), "sec"),
            (nameof(WorkbenchStrings.ThemeTertiary), "ter"),
            (nameof(WorkbenchStrings.ThemeInfo), "inf"),
            (nameof(WorkbenchStrings.ThemeSuccess), "suc"),
            (nameof(WorkbenchStrings.ThemeWarning), "wrn"),
            (nameof(WorkbenchStrings.ThemeSevereWarning), "swr"),
            (nameof(WorkbenchStrings.ThemeError), "err")
        }.Select(role => (role.NameKey, roleSlots.Select(slot => ThemeToken.Color($"--bit-clr-{role.Code}{slot}")).ToArray()))
    ];

    /// <summary>The text, background and border colors, each tier with its hovered and pressed shade, with the resource key of the family's name.</summary>
    public static readonly (string NameKey, ThemeToken[] Tokens)[] Neutrals =
    [
        (nameof(WorkbenchStrings.ThemeForeground), Tiers("fg", [])),
        (nameof(WorkbenchStrings.ThemeBackground), Tiers("bg", ["--bit-clr-bg-overlay"])),
        (nameof(WorkbenchStrings.ThemeBorder), Tiers("brd", []))
    ];

    /// <summary>The gray ramp the neutral colors are picked from.</summary>
    public static readonly ThemeToken[] Grays =
    [
        ThemeToken.Color("--bit-clr-ntr-white"),
        .. Enumerable.Range(1, 22).Select(step => ThemeToken.Color($"--bit-clr-ntr-gray{step * 10}")),
        ThemeToken.Color("--bit-clr-ntr-black")
    ];

    /// <summary>Colors of one purpose each: the required-field marker and the tooltip.</summary>
    public static readonly ThemeToken[] SpecialColors =
    [
        ThemeToken.Color("--bit-clr-req"),
        ThemeToken.Color("--bit-clr-tooltip-bg"),
        ThemeToken.Color("--bit-clr-tooltip-fg")
    ];

    /// <summary>The intent tier: names for what a color is for, which app styles can read instead of a role.</summary>
    public static readonly ThemeToken[] Semantic =
    [
        .. new[] { "surface-page", "surface-elevated", "surface-muted", "text-primary", "text-secondary", "border-default", "accent-primary", "focus-color" }
            .Select(name => ThemeToken.Color($"--bit-sem-{name}"))
    ];

    public static readonly ThemeToken[] FontFamilies = [ThemeToken.Raw("--bit-tpg-font-family"), ThemeToken.Raw("--bit-tpg-font-family-mono")];

    public static readonly ThemeToken[] FontSizes = [.. new[] { "2xs", "xs", "sm", "md", "lg", "xl", "2xl", "3xl", "4xl" }.Select(size => ThemeToken.Length($"--bit-tpg-fs-{size}"))];

    public static readonly ThemeToken[] FontWeights = [.. new[] { "light", "regular", "medium", "semibold", "bold" }.Select(weight => ThemeToken.Raw($"--bit-tpg-fw-{weight}"))];

    /// <summary>The base text settings and the ones controls, dialog titles and field labels take.</summary>
    public static readonly ThemeToken[] TextSettings =
    [
        ThemeToken.Raw("--bit-tpg-font-weight"),
        ThemeToken.Raw("--bit-tpg-line-height"),
        ThemeToken.Raw("--bit-tpg-gutter-size"),
        ThemeToken.Raw("--bit-tpg-ctrl-letter-spacing"),
        ThemeToken.Raw("--bit-tpg-ctrl-text-transform"),
        ThemeToken.Length("--bit-tpg-dialog-title-font-size"),
        ThemeToken.Raw("--bit-tpg-dialog-title-font-weight"),
        ThemeToken.Raw("--bit-tpg-field-label-font-weight")
    ];

    /// <summary>The corner radii: the scale, then the radius each family of components takes.</summary>
    public static readonly ThemeTokenGroup[] Radii =
    [
        new(nameof(WorkbenchStrings.ThemeTokensScale), [.. new[] { "none", "xs", "sm", "md", "lg", "xl", "2xl", "full" }.Select(name => ThemeToken.Length($"--bit-shp-radius-{name}"))]),
        new(nameof(WorkbenchStrings.ThemeTokensFamilies), [.. new[] { "control", "button", "chip", "selection", "surface", "popup", "dialog", "sheet", "tab-indicator", "progress" }.Select(name => ThemeToken.Length($"--bit-shp-radius-{name}"))])
    ];

    /// <summary>The borders and the focus ring.</summary>
    public static readonly ThemeToken[] Strokes =
    [
        ThemeToken.Length("--bit-shp-brd-width"),
        ThemeToken.Length("--bit-shp-brd-width-thick"),
        ThemeToken.Raw("--bit-shp-brd-style"),
        ThemeToken.Raw("--bit-shp-brd-radius"),
        ThemeToken.Length("--bit-shp-focus-ring-width"),
        ThemeToken.Length("--bit-shp-focus-ring-offset")
    ];

    /// <summary>The focus rings drawn as shadows.</summary>
    public static readonly ThemeToken[] FocusRings = [ThemeToken.Raw("--bit-shd-focus-ring"), ThemeToken.Raw("--bit-sem-focus-ring")];

    /// <summary>The shadows: the one each surface lifts with, the general scale, and the 24 elevation steps.</summary>
    public static readonly ThemeTokenGroup[] Shadows =
    [
        new(nameof(WorkbenchStrings.ThemeSurfaces), [.. new[] { "card", "card-hover", "popup", "dialog", "sheet", "tooltip", "snackbar", "appbar-top", "appbar-bottom" }.Select(name => ThemeToken.Raw($"--bit-shd-{name}"))]),
        new(nameof(WorkbenchStrings.ThemeTokensScale), [.. new[] { "sm", "nm", "md", "lg", "xl", "2xl", "inner", "cal", "cal2" }.Select(name => ThemeToken.Raw($"--bit-shd-{name}"))]),
        new(nameof(WorkbenchStrings.ThemeTokensElevation), [.. Enumerable.Range(1, 24).Select(step => ThemeToken.Raw($"--bit-shd-{step}"))])
    ];

    /// <summary>The sizes components are built from, a group per part.</summary>
    public static readonly ThemeTokenGroup[] Sizes =
    [
        new(nameof(WorkbenchStrings.ThemeTokensControlHeight), Scaled("--bit-siz-ctrl-sm", "--bit-siz-ctrl-md", "--bit-siz-ctrl-lg")),
        new(nameof(WorkbenchStrings.ThemeTokensControlPadding), Scaled("--bit-siz-ctrl-pad-x-sm", "--bit-siz-ctrl-pad-x-md", "--bit-siz-ctrl-pad-x-lg", "--bit-siz-ctrl-pad-y-sm", "--bit-siz-ctrl-pad-y-md", "--bit-siz-ctrl-pad-y-lg", "--bit-siz-ctrl-min-width")),
        new(nameof(WorkbenchStrings.ThemeTokensIcons), Lengths("--bit-siz-icon-sm", "--bit-siz-icon-md", "--bit-siz-icon-lg")),
        new(nameof(WorkbenchStrings.ThemeTokensCheckboxesAndRadios), Scaled("--bit-siz-sel-sm", "--bit-siz-sel-md", "--bit-siz-sel-lg")),
        new(nameof(WorkbenchStrings.ThemeTokensListRows), Scaled("--bit-siz-item-sm", "--bit-siz-item-md", "--bit-siz-item-lg")),
        new(nameof(WorkbenchStrings.ThemeTokensTabs), [ThemeToken.Scaled("--bit-siz-tab"), ThemeToken.Length("--bit-siz-tab-indicator"), ThemeToken.Length("--bit-siz-tab-divider")]),
        new(nameof(WorkbenchStrings.ThemeTokensLinesAndTracks), Lengths("--bit-siz-divider", "--bit-siz-spinner-stroke", "--bit-siz-track-sm", "--bit-siz-track-md", "--bit-siz-track-lg")),
        new(nameof(WorkbenchStrings.ThemeTokensSwitches), Scaled("--bit-siz-switch-w-sm", "--bit-siz-switch-w-md", "--bit-siz-switch-w-lg", "--bit-siz-switch-h-sm", "--bit-siz-switch-h-md", "--bit-siz-switch-h-lg", "--bit-siz-switch-thumb-sm", "--bit-siz-switch-thumb-md", "--bit-siz-switch-thumb-lg")),
        new(nameof(WorkbenchStrings.ThemeTokensSliderThumbs), Scaled("--bit-siz-slider-thumb-sm", "--bit-siz-slider-thumb-md", "--bit-siz-slider-thumb-lg")),
        new(nameof(WorkbenchStrings.ThemeTokensBadges), Scaled("--bit-siz-badge-sm", "--bit-siz-badge-md", "--bit-siz-badge-lg", "--bit-siz-badge-dot-sm", "--bit-siz-badge-dot-md", "--bit-siz-badge-dot-lg")),
        new(nameof(WorkbenchStrings.ThemeInputsChips), Scaled("--bit-siz-chip-sm", "--bit-siz-chip-md", "--bit-siz-chip-lg")),
        new(nameof(WorkbenchStrings.ThemeTokensLimits), Scaled("--bit-siz-popup-max-height", "--bit-siz-dialog-max-width"))
    ];

    /// <summary>The spacing unit, the density that scales it, and the insets of cards and dialogs.</summary>
    public static readonly ThemeToken[] Spacing =
    [
        ThemeToken.Length("--bit-spa-scaling-factor"),
        ThemeToken.Raw("--bit-layout-density-scale"),
        .. Scaled("--bit-spa-card-sm", "--bit-spa-card-md", "--bit-spa-card-lg", "--bit-spa-dialog")
    ];

    public static readonly ThemeToken[] Durations =
    [
        .. new[] { "--bit-mot-duration-short", "--bit-mot-duration", "--bit-mot-duration-long", "--bit-mot-duration-spinner", "--bit-mot-duration-factor", "--bit-mot-loop-factor" }.Select(ThemeToken.Raw)
    ];

    public static readonly ThemeToken[] Easings =
    [
        .. new[] { "--bit-mot-easing", "--bit-mot-easing-decelerate", "--bit-mot-easing-accelerate", "--bit-mot-easing-spinner" }.Select(ThemeToken.Raw)
    ];

    /// <summary>The stacking order of the layers that float over the page, lowest first.</summary>
    public static readonly ThemeToken[] Layers = [.. new[] { "base", "callout", "overlay", "modal", "snackbar" }.Select(name => ThemeToken.Raw($"--bit-zin-{name}"))];

    public static readonly ThemeToken[] Breakpoints = [.. new[] { "xs", "sm", "md", "lg", "xl", "xxl" }.Select(name => ThemeToken.Raw($"--bit-bp-{name}"))];

    /// <summary>How a dialog lays out its text and its buttons.</summary>
    public static readonly ThemeToken[] DialogLayout =
    [
        .. new[] { "--bit-layout-dialog-actions-direction", "--bit-layout-dialog-actions-justify", "--bit-layout-dialog-actions-align", "--bit-layout-dialog-text-align" }.Select(ThemeToken.Raw)
    ];

    public static readonly ThemeToken DisabledOpacity = ThemeToken.Raw("--bit-opa-dis");

    /// <summary>Every token the page shows, each once.</summary>
    public static IEnumerable<ThemeToken> All =>
        new[]
        {
            Roles.SelectMany(role => role.Tokens), Neutrals.SelectMany(family => family.Tokens), Grays, SpecialColors, Semantic,
            FontFamilies, FontSizes, FontWeights, TextSettings, Radii.SelectMany(group => group.Tokens), Strokes, FocusRings,
            Shadows.SelectMany(group => group.Tokens), Sizes.SelectMany(group => group.Tokens), Spacing, Durations, Easings,
            Layers, Breakpoints, DialogLayout, [DisabledOpacity]
        }.SelectMany(tokens => tokens).DistinctBy(token => token.Name);

    /// <summary>What the browser is asked to resolve for every token.</summary>
    public static ThemeTokenRequest[] Requests { get; } = [.. All.Select(token => new ThemeTokenRequest(token.Name, token.Kind.ToString().ToLowerInvariant(), token.Css))];

    private static ThemeToken[] Tiers(string family, string[] extra) =>
    [
        .. new[] { "pri", "sec", "ter" }.SelectMany(tier => new[] { "", "-hover", "-active" }.Select(state => ThemeToken.Color($"--bit-clr-{family}-{tier}{state}"))),
        ThemeToken.Color($"--bit-clr-{family}-dis"),
        .. extra.Select(ThemeToken.Color)
    ];

    private static ThemeToken[] Scaled(params string[] names) => [.. names.Select(ThemeToken.Scaled)];

    private static ThemeToken[] Lengths(params string[] names) => [.. names.Select(ThemeToken.Length)];
}
