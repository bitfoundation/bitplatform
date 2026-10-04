namespace Bit.BlazorUI;

/// <summary>Tooltip plugin options.</summary>
public sealed class BitChartTooltipOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Overrides <see cref="BitChartInteractionOptions.Mode"/> for the tooltip only.</summary>
    public BitChartInteractionMode? Mode { get; set; }

    /// <summary>Overrides <see cref="BitChartInteractionOptions.Intersect"/> for the tooltip only.</summary>
    public bool? Intersect { get; set; }

    /// <summary>Where the tooltip is anchored when multiple items are active.</summary>
    public BitChartTooltipPositioner Position { get; set; } = BitChartTooltipPositioner.Average;
    /// <summary>Fill of the tooltip box (and its caret). Defaults to the theme's tooltip surface.</summary>
    public string BackgroundColor { get; set; } = "var(--bit-Chart-tooltip-background, var(--bit-clr-tooltip-bg))";
    /// <summary>Color of the tooltip title. Defaults to the theme's tooltip text color.</summary>
    public string TitleColor { get; set; } = DefaultTextColor;
    /// <summary>Color of the tooltip body lines. Defaults to the theme's tooltip text color.</summary>
    public string BodyColor { get; set; } = DefaultTextColor;
    /// <summary>Color of the tooltip footer. Defaults to the theme's tooltip text color.</summary>
    public string FooterColor { get; set; } = DefaultTextColor;

    private const string DefaultTextColor = "var(--bit-Chart-tooltip-color, var(--bit-clr-tooltip-fg))";
    public BitChartFont TitleFont { get; set; } = new() { Weight = "bold" };
    public BitChartFont BodyFont { get; set; } = new();
    public BitChartFont FooterFont { get; set; } = new() { Weight = "bold" };
    public double Padding { get; set; } = 6;

    /// <summary>
    /// Caps the tooltip's width in pixels and wraps its text at that point. Without one the box stays on
    /// a single line per row, which is right for a value but wrong for a sentence, so a callback that
    /// returns prose wants a width here.
    /// </summary>
    public double? MaxWidth { get; set; }
    /// <summary>
    /// Corner radius of the tooltip box in pixels. Null (the default) follows the theme's popup radius,
    /// read through the public <c>--bit-Chart-tooltip-radius</c> custom property.
    /// </summary>
    public double? CornerRadius { get; set; }
    public bool DisplayColors { get; set; } = true;
    /// <summary>Render the color swatch using the dataset point style instead of a square.</summary>
    public bool UsePointStyle { get; set; }
    /// <summary>Border color of the tooltip box.</summary>
    public string? BorderColor { get; set; }
    /// <summary>Border width of the tooltip box.</summary>
    public double BorderWidth { get; set; }
    /// <summary>Draw a caret (arrow) pointing at the anchored element. Chart.js draws one by default.</summary>
    public bool Caret { get; set; } = true;
    /// <summary>Size of the caret in pixels.</summary>
    public double CaretSize { get; set; } = 6;
    /// <summary>Text alignment of the title (left/center/right).</summary>
    public BitChartAlign TitleAlign { get; set; } = BitChartAlign.Start;
    /// <summary>Text alignment of the body (left/center/right).</summary>
    public BitChartAlign BodyAlign { get; set; } = BitChartAlign.Start;
    /// <summary>Rich text/styling callbacks.</summary>
    public BitChartTooltipCallbacks Callbacks { get; set; } = new();
    /// <summary>Optional label formatter: (datasetLabel, value) => text. Shorthand for <c>Callbacks.Label</c>.</summary>
    public Func<string, double, string>? LabelFormatter { get; set; }
    /// <summary>Filters which active items are listed, mirroring Chart.js <c>tooltip.filter</c>.</summary>
    public Func<BitChartTooltipItemContext, bool>? Filter { get; set; }
    /// <summary>Sorts the listed items, mirroring Chart.js <c>tooltip.itemSort</c>.</summary>
    public Comparison<BitChartTooltipItemContext>? ItemSort { get; set; }
}
