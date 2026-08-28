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

    /// <summary>The geometry of a switch (<c>BitToggle</c>) per size class - the track and the knob (<c>--bit-siz-switch-*</c>).</summary>
    public BitThemeSwitchSizes Switch { get; set; } = new();

    /// <summary>The draggable handle of a <c>BitSlider</c> per size class (<c>--bit-siz-slider-thumb-{sm,md,lg}</c>).</summary>
    public BitThemeSizeScale SliderThumb { get; set; } = new();

    /// <summary>The stroke of every inline circular spinner (<c>--bit-siz-spinner-stroke</c>).</summary>
    public string? SpinnerStroke { get; set; }

    /// <summary>The tallest a popup list (dropdown, search suggestions, breadcrumb overflow) grows before it scrolls (<c>--bit-siz-popup-max-height</c>).</summary>
    public string? PopupMaxHeight { get; set; }

    /// <summary>The widest a dialog grows on its own before its message wraps (<c>--bit-siz-dialog-max-width</c>); a dialog given a width or a max width of its own ignores it.</summary>
    public string? DialogMaxWidth { get; set; }
}

/// <summary>
/// The geometry of a switch (<c>BitToggle</c>): the track it slides in and the knob that slides
/// (Fluent's 40x20 pill with its 12px knob, Material's 52x32 with a 24dp handle, the 51x31 UISwitch
/// with its 27pt thumb). The inset that holds the knob off the track edge is not a token: the
/// component derives it as (height - knob) / 2 less the border width, so the knob keeps the same
/// distance from the stroke on every side of whatever geometry a theme sets.
/// </summary>
public class BitThemeSwitchSizes
{
    /// <summary>The width of the track per size class (<c>--bit-siz-switch-w-{sm,md,lg}</c>).</summary>
    public BitThemeSizeScale Width { get; set; } = new();

    /// <summary>The height of the track per size class (<c>--bit-siz-switch-h-{sm,md,lg}</c>).</summary>
    public BitThemeSizeScale Height { get; set; } = new();

    /// <summary>The size of the knob per size class (<c>--bit-siz-switch-thumb-{sm,md,lg}</c>).</summary>
    public BitThemeSizeScale Thumb { get; set; } = new();
}

/// <summary>A small / medium / large triple of CSS lengths.</summary>
public class BitThemeSizeScale
{
    public string? Sm { get; set; }
    public string? Md { get; set; }
    public string? Lg { get; set; }
}
