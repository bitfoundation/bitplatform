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
    /// <summary>Axis/axes affected by zoom and pan.</summary>
    public BitChartZoomMode Mode { get; set; } = BitChartZoomMode.X;
    /// <summary>Wheel zoom sensitivity (fraction per wheel notch).</summary>
    public double Speed { get; set; } = 0.15;
}
