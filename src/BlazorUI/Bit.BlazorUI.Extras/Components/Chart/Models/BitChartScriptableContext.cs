namespace Bit.BlazorUI;

/// <summary>
/// Contextual information passed to scriptable dataset options, mirroring Chart.js
/// <c>BitChartScriptableContext</c>. A scriptable option is a <c>Func&lt;BitChartScriptableContext, T&gt;</c> that
/// is evaluated per data element, letting colors/radius/styles vary by value, index or state.
/// </summary>
public sealed class BitChartScriptableContext
{
    /// <summary>Index of the dataset this element belongs to.</summary>
    public int DatasetIndex { get; init; }

    /// <summary>Index of the data point within the dataset.</summary>
    public int DataIndex { get; init; }

    /// <summary>The parsed primary value (y for cartesian, value for arcs/radar).</summary>
    public double? Value { get; init; }

    /// <summary>The parsed x value for point-based datasets (scatter/bubble), else null.</summary>
    public double? ValueX { get; init; }

    /// <summary>The bubble radius value for bubble datasets, else null.</summary>
    public double? ValueR { get; init; }

    /// <summary>The category label for this index, if any.</summary>
    public string? Label { get; init; }

    /// <summary>The dataset label.</summary>
    public string? DatasetLabel { get; init; }

    /// <summary>True when this element is currently active (hovered/focused).</summary>
    public bool Active { get; init; }

    /// <summary>The effective chart type for this dataset.</summary>
    public BitChartType Type { get; init; }
}
