namespace Bit.BlazorUI;

/// <summary>Zoom &amp; pan options, mirroring chartjs-plugin-zoom. Cartesian charts only.</summary>
public sealed class BitChartZoomOptions
{
    public bool Enabled { get; set; }
    /// <summary>Enable mouse-wheel zoom.</summary>
    public bool Wheel { get; set; } = true;
    /// <summary>Enable click-and-drag panning.</summary>
    public bool Pan { get; set; } = true;
    /// <summary>Enable drag-to-zoom box selection (overrides <see cref="Pan"/> for the drag gesture).</summary>
    public bool DragZoom { get; set; }
    /// <summary>Fill color of the drag-zoom selection box.</summary>
    public string DragBoxColor { get; set; } = "rgba(54,162,235,0.2)";
    /// <summary>Border color of the drag-zoom selection box.</summary>
    public string DragBoxBorderColor { get; set; } = "rgba(54,162,235,0.8)";
    /// <summary>Axis/axes affected by zoom and pan.</summary>
    public BitChartZoomMode Mode { get; set; } = BitChartZoomMode.X;
    /// <summary>Wheel zoom sensitivity (fraction per wheel notch).</summary>
    public double Speed { get; set; } = 0.15;
    /// <summary>
    /// When true (the default) zooming and panning stay inside the data range, so the chart can never
    /// be dragged into empty space or zoomed out past the full series.
    /// </summary>
    public bool LimitToData { get; set; } = true;
    /// <summary>Smallest visible span, as a fraction of the full data range (guards against zooming in forever).</summary>
    public double MinRangeFraction { get; set; } = 0.001;
}
