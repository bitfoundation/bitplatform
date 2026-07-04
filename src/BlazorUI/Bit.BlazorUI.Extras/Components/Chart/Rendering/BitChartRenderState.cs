namespace Bit.BlazorUI;

/// <summary>Mutable interaction state shared with the component (hover + legend toggles).</summary>
public sealed class BitChartRenderState
{
    /// <summary>Datasets hidden via the legend (by dataset index).</summary>
    public HashSet<int> HiddenDatasets { get; } = new();

    /// <summary>Data indices hidden via the legend (pie/doughnut/polarArea).</summary>
    public HashSet<int> HiddenIndices { get; } = new();

    /// <summary>The currently hovered element, if any.</summary>
    public (int Dataset, int Index)? Active { get; set; }

    /// <summary>Zoom/pan range overrides per axis id (min, max in data coordinates).</summary>
    public Dictionary<string, (double Min, double Max)> AxisRanges { get; } = new();

    public bool IsDatasetHidden(int i) => HiddenDatasets.Contains(i);
    public bool IsIndexHidden(int i) => HiddenIndices.Contains(i);
}
