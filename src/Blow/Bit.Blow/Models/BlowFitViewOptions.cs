namespace Bit.Blow;

/// <summary>
/// Options for <c>BlowCanvas.FitView</c>. Mirrors React Flow's <c>FitViewOptions</c>.
/// </summary>
public sealed class BlowFitViewOptions
{
    /// <summary>
    /// Fraction of the pane left empty around the fitted content, 0..1. The default 0.1
    /// leaves a 10% breathing space.
    /// </summary>
    public double Padding { get; set; } = 0.1;

    /// <summary>When true, hidden nodes are taken into account when computing the bounds.</summary>
    public bool IncludeHiddenNodes { get; set; }

    /// <summary>Lower zoom bound for this call. Never goes below the canvas's own MinZoom.</summary>
    public double? MinZoom { get; set; }

    /// <summary>Upper zoom bound for this call. Never goes above the canvas's own MaxZoom.</summary>
    public double? MaxZoom { get; set; }

    /// <summary>Fit only these nodes instead of the whole graph.</summary>
    public IEnumerable<BlowNode>? Nodes { get; set; }

    /// <summary>
    /// Milliseconds the viewport takes to ease into place, when the fit is requested through
    /// <c>FitViewAsync</c>. Zero (the default) applies it immediately. Ignored by the
    /// synchronous <c>FitView</c>, which never animates.
    /// </summary>
    public int Duration { get; set; }
}
