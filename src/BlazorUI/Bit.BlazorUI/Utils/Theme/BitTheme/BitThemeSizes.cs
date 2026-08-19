namespace Bit.BlazorUI;

/// <summary>
/// The dimensions a design system decides once and every component repeats (<c>--bit-siz-*</c>):
/// control heights, icon glyph sizes, selection-indicator sizes, the spinner stroke and the maximum
/// height of a popup list.
/// </summary>
/// <remarks>
/// In the shipped stylesheets the heights and selection sizes are density-aware (multiples of
/// <see cref="BitThemeSpacings.ScalingFactor"/> times <see cref="BitThemeLayout.DensityScale"/>), so
/// the density presets keep working; a theme built for another design system may set absolute
/// lengths instead (Material: <c>40px</c> buttons / <c>56px</c> fields, Cupertino: <c>44px</c>).
/// </remarks>
public class BitThemeSizes
{
    /// <summary>Heights of text fields, dropdowns, pickers, search boxes and icon-only buttons per size class (<c>--bit-siz-ctrl-{sm,md,lg}</c>).</summary>
    public BitThemeSizeScale Control { get; set; } = new();

    /// <summary>Inner horizontal padding of padded controls (buttons, toggle buttons, menu buttons) per size class (<c>--bit-siz-ctrl-pad-x-{sm,md,lg}</c>).</summary>
    public BitThemeSizeScale ControlPaddingX { get; set; } = new();

    /// <summary>Inner vertical padding of padded controls per size class (<c>--bit-siz-ctrl-pad-y-{sm,md,lg}</c>).</summary>
    public BitThemeSizeScale ControlPaddingY { get; set; } = new();

    /// <summary>The minimum width of a labeled button (<c>--bit-siz-ctrl-min-width</c>; Fluent imposes none, Material mandates 64px).</summary>
    public string? ControlMinWidth { get; set; }

    /// <summary>Icon glyph sizes inside controls per size class (<c>--bit-siz-icon-{sm,md,lg}</c>).</summary>
    public BitThemeSizeScale Icon { get; set; } = new();

    /// <summary>The checkbox box and radio ring sizes per size class (<c>--bit-siz-sel-{sm,md,lg}</c>).</summary>
    public BitThemeSizeScale Selection { get; set; } = new();

    /// <summary>Row heights of the item lists inside popups (dropdown options, menu items, breadcrumb overflow) per size class (<c>--bit-siz-item-{sm,md,lg}</c>).</summary>
    public BitThemeSizeScale Item { get; set; } = new();

    /// <summary>The height of a tab (pivot) header (<c>--bit-siz-tab</c>).</summary>
    public string? Tab { get; set; }

    /// <summary>The thickness of the tab selection indicator (<c>--bit-siz-tab-indicator</c>).</summary>
    public string? TabIndicator { get; set; }

    /// <summary>The thickness of a divider rule - separators and menu dividers (<c>--bit-siz-divider</c>; defaults to the hairline border width).</summary>
    public string? Divider { get; set; }

    /// <summary>The thickness of a linear track (progress bar) per size class (<c>--bit-siz-track-{sm,md,lg}</c>).</summary>
    public BitThemeSizeScale Track { get; set; } = new();

    /// <summary>The stroke of every inline circular spinner (<c>--bit-siz-spinner-stroke</c>).</summary>
    public string? SpinnerStroke { get; set; }

    /// <summary>The tallest a popup list (dropdown, search suggestions, breadcrumb overflow) grows before it scrolls (<c>--bit-siz-popup-max-height</c>).</summary>
    public string? PopupMaxHeight { get; set; }
}

/// <summary>A small / medium / large triple of CSS lengths.</summary>
public class BitThemeSizeScale
{
    public string? Sm { get; set; }
    public string? Md { get; set; }
    public string? Lg { get; set; }
}
