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
    public string BackgroundColor { get; set; } = "rgba(0,0,0,0.8)";
    public string TitleColor { get; set; } = "#fff";
    public string BodyColor { get; set; } = "#fff";
    public string FooterColor { get; set; } = "#fff";
    public BitChartFont TitleFont { get; set; } = new() { Weight = "bold" };
    public BitChartFont BodyFont { get; set; } = new();
    public BitChartFont FooterFont { get; set; } = new() { Weight = "bold" };
    public double Padding { get; set; } = 6;
    public double CornerRadius { get; set; } = 6;
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
